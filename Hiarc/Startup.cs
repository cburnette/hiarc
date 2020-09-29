using System.Security.Claims;
using System.Text;
using Hiarc.Core.Database;
using Hiarc.Core.Events;
using Hiarc.Core.Settings;
using Hiarc.Core.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Hiarc
{
    public class Startup
    {
        public const string HIARC_SECTION_KEY = "Hiarc";
        public const string HIARC_JWT_SIGNING_KEY_KEY = "Hiarc:JwtSigningKey";
        public const string HIARC_ADMIN_API_KEY = "Hiarc:AdminApiKey";
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
                .AddApiKeySupport(options => {})
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
