using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Azure.Core;
using FiveDegrees.Audit.Http.Extensions;
using GraphQL.Server.Ui.Playground;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ProcessManager.API.Extensions;
using ProcessManager.Infrastructure.Models;
using ProcessManager.Infrastructure.Providers;
using ProcessManager.Infrastructure.Services;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.ApplicationInsights;

namespace ProcessManager.API
{
    public class Startup
    {
        private static readonly string _assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();

            if (!string.IsNullOrWhiteSpace(Configuration.GetValue<string>("APPINSIGHTS_INSTRUMENTATIONKEY")))
            {
                services.AddSingleton<ILogEventSink>(
                    p => new ApplicationInsightsSink(
                                p.GetRequiredService<TelemetryClient>(),
                                TelemetryConverter.Traces));
            }

            services.AddHealthChecks()
                .AddCheck(
                    name: "ProcessManager Api",
                    () => HealthCheckResult.Healthy("Process Manager Api is alive"),
                    tags: new[] { "liveness", "api" })
                .AddDbContextCheck<ProcesManagerDbContext>(tags: new[] { "readiness", "api" });

            services.AddSingleton<TelemetryConfiguration>(sp =>
            {
                var key = Configuration.GetConnectionString("APPINSIGHTS_INSTRUMENTATIONKEY");
                if (!string.IsNullOrWhiteSpace(key))
                {
                    return new TelemetryConfiguration(key);
                }
                return new TelemetryConfiguration();
            });

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = _assemblyName, Version = "v1" });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{_assemblyName}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            TokenCredential tokenCredential = AzureCredentials.GetCliCredentials();

            services.AddAuditMiddleware(
                options =>
                {
                    options.ServiceBusConnectionString = Configuration.GetConnectionString("ServiceBusConnectionString");
                    options.TokenProvider = new AzureIdentityServiceBusCredentialAdapter(tokenCredential);
                    options.PathsToIgnore = new List<string>()
                    {
                        "/health/liveness",
                        "/health/readiness"
                    };
                    options.Enabled = Configuration.GetValue<bool>("ProcessManagerConfiguration:AuditEnabled");
                });

            RegisterDbContext(services);

            var mvcBuilder = services.AddControllers();

            services.AddGraphQLEntityFramework(mvcBuilder);
        }

        protected virtual void RegisterDbContext(IServiceCollection services)
        {
            services.AddDbContext<ProcesManagerDbContext>(opt =>
                opt.UseSqlServer(Configuration.GetConnectionString("ProcessManagerDbConnectionString")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{_assemblyName} V1");
            });
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseGraphQLPlayground(options: new GraphQLPlaygroundOptions
            {
                GraphQLEndPoint = "/api/graphql",
                Headers = new Dictionary<string, object>()
                {
                    { "GraphQlPlayground", true }
                }
            });

            app.UseRouting();

            ConfigureAuditMiddleware(app);

            if (!string.Equals(Configuration.GetValue<string>("ResourceLevel").ToLowerInvariant(), "platform"))
            {
                ConfigureAuthorization(app);
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        protected virtual void ConfigureAuditMiddleware(IApplicationBuilder app)
        {
            app.UseAuditMiddleware();
        }

        protected virtual void ConfigureAuthorization(IApplicationBuilder app)
        {
            app.UseAuthentication();
        }
    }
}
