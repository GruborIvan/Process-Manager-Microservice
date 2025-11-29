using Autofac;
using Autofac.Extensions.DependencyInjection;
using FiveDegrees.Audit.Rebus.Extensions;
using FiveDegrees.Audit.Rebus.HttpClientMessageHandlers;
using Matrix.HealthCheckers.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using ProcessManager.BackgroundWorker.HealthChecks;
using ProcessManager.BackgroundWorker.Modules;
using ProcessManager.Infrastructure.HealthChecks;
using ProcessManager.Infrastructure.Models;
using ProcessManager.Infrastructure.Modules;
using Serilog;
using System;
using System.IO;
using Microsoft.FeatureManagement;
using Azure.Core;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProcessManager.Domain.Interfaces;
using ProcessManager.BackgroundWorker.Helpers;
using ProcessManager.Infrastructure.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog.Core;
using Serilog.Sinks.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace ProcessManager.BackgroundWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            CreateHostBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>((hostContext, builder) =>
                {
                    var config = hostContext.Configuration;
                    builder.RegisterModule(new RebusModule(config));
                    builder.RegisterModule(new InfrastructureModule(config));
                    builder.RegisterModule(new MediatRModule());
                    builder.RegisterModule(new AutoMapperModule(typeof(InfrastructureModule).Assembly, new RebusContextAccessor()));
                })
                .Build()
                .Run();
        }

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((context, services, loggerConfiguration) =>
                    loggerConfiguration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services))
                .ConfigureAppConfiguration((context, config) =>
                {
                    TokenCredential credentials = AzureCredentials.GetDefaultCredentials();
                    var configuration = config.Build();

                    config.AddAzureAppConfiguration(options =>
                    {
                        options.Connect(new Uri(configuration["AppConfigurationEndpoint"]), credentials)
                            .UseFeatureFlags()
                            .ConfigureKeyVault(kv =>
                            {
                                kv.SetCredential(credentials);
                            });
                    });
                }
                )
                .ConfigureServices((hostContext, services) =>
                {
                    var config = hostContext.Configuration;
                    
                    services.AddApplicationInsightsTelemetry();
                    TokenCredential credentials = AzureCredentials.GetCliCredentials();

                    services.AddSingleton<TelemetryConfiguration>(sp =>
                    {
                        var key = config["APPINSIGHTS_INSTRUMENTATIONKEY"];
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            return new TelemetryConfiguration(key);
                        }
                        return new TelemetryConfiguration();
                    });

                    if (!string.IsNullOrWhiteSpace(Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]))
                    {
                        services.AddSingleton<ILogEventSink>(
                            p => new ApplicationInsightsSink(
                                        p.GetRequiredService<TelemetryClient>(),
                                        TelemetryConverter.Traces));
                    }

                    services.AddHostedService<Worker>();
                    services.AddHealthChecks()
                            .AddDbContextCheck<ProcesManagerDbContext>(tags: new[] { "liveness", "api" })
                            .AddAzureServiceBusQueue(
                                uri: Configuration.GetValue<string>("ProcessManagerConfiguration:ServiceBusUri"),
                                queueName: Configuration.GetValue<string>("ProcessManagerConfiguration:ServiceBusQueueName"),
                                tokenCredential: credentials,
                                tags: new[] { "liveness", "api" })
                            .AddServiceHealthCheck<LogicAppServiceHealthChecker>(
                                name: "Logic App definitions backend",
                                tags: new[] { "liveness", "api" });

                    if (!string.Equals(config.GetValue<string>("ResourceLevel").ToLowerInvariant(), "platform"))
                    {
                        services.AddHealthChecks()
                            .AddServiceHealthCheck<ServiceHealthChecker>(
                                name: "Configuration Manager Service",
                                tags: new[] { "liveness", "api" },
                                args: new object[]
                                {
                                    "CMService",
                                    string.Concat(config.GetValue<string>("General:ApiManagement:ApimBaseUrl"), "/", config.GetValue<string>("ProcessManagerConfiguration:ConfigurationManagerBaseUrl")),
                                    config.GetValue<string>("General:ApiManagement:TntInternalApiSubscriptionKey"),
                                    config.GetValue<string>("AuthorizationConfiguration:AuthScope"),
                                    new AzureTokenProvider(new AzureServiceTokenProvider())
                                });
                    }

                    services.Configure<HealthCheckPublisherOptions>(options =>
                    {
                        options.Delay = TimeSpan.FromSeconds(10);
                        options.Predicate = (check) => check.Tags.Contains("liveness");
                    });

                    services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();
                    services.TryAddScoped<IContextAccessor, RebusContextAccessor>();

                    services.AddRebusContextHandler();

                    services.AddHttpClient(string.Empty, client =>
                    {
                    }).AddHttpMessageHandler<RebusContextHandler>();
                    services.AddDbContext<ProcesManagerDbContext>(opt =>
                        opt.UseSqlServer(config.GetConnectionString("ProcessManagerDbConnectionString")));

                    services.AddFeatureManagement();
                });
    }
}
