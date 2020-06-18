using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Hiarc.Core.Database;
using Hiarc.Core.Events;
using Hiarc.Core.Settings;
using Hiarc.Core.Storage;

namespace HiarcGRPC
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

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<HiarcSettings>(Configuration.GetSection(HIARC_SECTION_KEY));

            services.AddSingleton<IHiarcDatabase, Neo4jDatabase>();

            // Add storage service providers (AWS-S3, Azure Blob Storage, Google Storage, etc.)
            services.AddSingleton<IStorageServiceProvider, StorageServiceProvider>();

            // Add event service providers (AWS Kinesis, Azure Service Bus, Google Pub/Sub, etc.)
            services.AddSingleton<IEventServiceProvider, HiarcEventServiceProvider>();

            services.AddGrpc();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<HiarcService2>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
