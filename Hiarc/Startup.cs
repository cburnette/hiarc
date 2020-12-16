using System;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Hiarc.Configuration.Strategies.Models;
using Hiarc.Core.Database;
using Hiarc.Core.Events;
using Hiarc.Core.Settings;
using Hiarc.Core.Storage;
using HiarcCore.Settings.KeyStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Hiarc
{
    public class Startup
    {
        public const string HIARC_SECTION_KEY = "Hiarc";
        public const string HIARC_KEYSTORE_KEY = "hiarcEncryptionKey";
        public const string HIARC_JWT_SIGNING_KEY_KEY = "Hiarc:JwtSigningKey";
        public const string HIARC_ADMIN_API_KEY = "Hiarc:AdminApiKey";
        public const string HIARC_KEYSTORE = "Hiarc:KeyStore";
        public const string HIARC_KEYSTORE_STORAGE = "Hiarc:KeyStore:StorageSettings";
        public const string HIARC_KEYSTORE_ENCRYPTION = "Hiarc:KeyStore:EncryptionSettings";
        public readonly static string HIARC_KEYSTORE_ENCRYPTION_PROVIDER = $"{HIARC_KEYSTORE_ENCRYPTION}:Provider";
        public readonly static string HIARC_KEYSTORE_ENCRYPTION_CONFIG = $"{HIARC_KEYSTORE_ENCRYPTION}:Config";
        public readonly static string HIARC_KEYSTORE_STORAGE_PROVIDER = $"{HIARC_KEYSTORE_STORAGE}:Provider";
        public readonly static string HIARC_KEYSTORE_STORAGE_CONFIG = $"{HIARC_KEYSTORE_STORAGE}:Config";
        public const string NEO4J_URI_KEY = "Neo4j:Uri";
        public const string NEO4J_USERNAME_KEY = "Neo4j:Username";
        public const string NEO4J_PASSWORD_KEY = "Neo4j:Password";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });

            services.AddHttpContextAccessor();

            services.Configure<HiarcSettings>(Configuration.GetSection(HIARC_SECTION_KEY));

            services.AddSingleton<IHiarcDatabase, Neo4jDatabase>();

            // Add storage service providers (AWS-S3, Azure Blob Storage, Google Storage, etc.)
            services.AddSingleton<IStorageServiceProvider, StorageServiceProvider>();

            // Add event service providers (AWS Kinesis, Azure Service Bus, Google Pub/Sub, etc.)
            services.AddSingleton<IEventServiceProvider, HiarcEventServiceProvider>();

            var jwtSigningSecret = Encoding.ASCII.GetBytes(Configuration.GetValue<string>(HIARC_JWT_SIGNING_KEY_KEY));

            services.AddAuthentication()
                .AddApiKeySupport(options => { })
                .AddJwtBearer(Auth.JWT_BEARER_SCHEME, options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(jwtSigningSecret),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(Auth.API_AUTHENTICATION_SCHEME, Auth.JWT_BEARER_SCHEME)
                    .Build();

                options.AddPolicy(Auth.REQUIRES_ADMIN, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(Auth.API_AUTHENTICATION_SCHEME)
                    .RequireClaim(ClaimTypes.Role, Auth.ADMIN_ROLE_CLAIM_VALUE)
                    .Build());
            });

            var ksStorageProvider = Configuration.GetValue<string>(HIARC_KEYSTORE_STORAGE_PROVIDER);
            var ksEncryptionProvider = Configuration.GetValue<string>(HIARC_KEYSTORE_ENCRYPTION_PROVIDER);
            byte[] encryptionCert;
            var encryptionCertPassword = "";
            if (ksEncryptionProvider != null && ksEncryptionProvider.ToLower() == KeyStoreServiceEncryptionSettings.INLINE)
            {
                var encodedEncryptionCert = Configuration.GetValue<string>($"{HIARC_KEYSTORE_ENCRYPTION_CONFIG}:EncodedCert");
                encryptionCertPassword = Configuration.GetValue<string>($"{HIARC_KEYSTORE_ENCRYPTION_CONFIG}:Password");
                try
                {
                    encryptionCert = Convert.FromBase64String(encodedEncryptionCert);
                }
                catch (FormatException)
                {
                    encryptionCert = Encoding.UTF8.GetBytes(encodedEncryptionCert);
                }
            }
            else
            {
                encryptionCert = new Byte[0];
            }
            if (ksStorageProvider.ToLower() == KeyStoreServiceStorageSettings.REDIS)
            {
                var redisConnection = Configuration.GetValue<string>($"{HIARC_KEYSTORE_STORAGE_CONFIG}:ConnectionString");
                var suffix = Configuration.GetValue<string>($"{HIARC_KEYSTORE_STORAGE_CONFIG}:KeySuffix");
                string redisKeySuffix = string.IsNullOrEmpty(suffix) ? "" : suffix;
                var redis = ConnectionMultiplexer.Connect(redisConnection);
                if (encryptionCert.Length > 0 && encryptionCertPassword != "")
                {
                    var cert = new X509Certificate2(encryptionCert, encryptionCertPassword);
                    services
                        .AddDataProtection()
                        .ProtectKeysWithCertificate(cert)
                        .PersistKeysToStackExchangeRedis(redis, $"{HIARC_KEYSTORE_KEY}{redisKeySuffix}");

                }
                else if (encryptionCert.Length > 0)
                {
                    var cert = new X509Certificate2(encryptionCert);
                    services
                        .AddDataProtection()
                        .ProtectKeysWithCertificate(cert)
                        .PersistKeysToStackExchangeRedis(redis, $"{HIARC_KEYSTORE_KEY}{redisKeySuffix}");

                }
                else
                {
                    services
                        .AddDataProtection()
                        .PersistKeysToStackExchangeRedis(redis, $"{HIARC_KEYSTORE_KEY}{redisKeySuffix}");
                }
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IOptions<HiarcSettings> hiarchSettings)
        {
            // The default is to NOT use Https redirection as Hiarc is most commonly accessed behind the firewall.
            // However, if you wish to allow direct access to Hiarc from mobile devices or Javascript you should set ForceHTTPS to true in your settings
            if (!string.IsNullOrEmpty(hiarchSettings.Value.ForceHTTPS) && hiarchSettings.Value.ForceHTTPS.ToLower() == "true")
            {
                app.UseHttpsRedirection();
            }

            app.UseHsts();
            app.UseRouting();
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
