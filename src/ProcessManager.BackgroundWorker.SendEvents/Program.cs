using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;
using ProcessManager.BackgroundWorker.SendEvents.HealthChecks;
using ProcessManager.BackgroundWorker.SendEvents.Modules;
using ProcessManager.Infrastructure.Models;
using ProcessManager.Infrastructure.Modules;
using ProcessManager.Infrastructure.Services;
using Serilog;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog.Core;
using Serilog.Sinks.ApplicationInsights;
using Microsoft.ApplicationInsights;

namespace ProcessManager.BackgroundWorker.SendEvents
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
                    builder.RegisterModule(new InfrastructureModule(config));
                    builder.RegisterModule(new MediatRModule());
                    builder.RegisterModule(new AutoMapperModule(typeof(InfrastructureModule).Assembly));
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
                            .AddDbContextCheck<ProcesManagerDbContext>(tags: new[] { "liveness", "api" });

                    services.Configure<HealthCheckPublisherOptions>(options =>
                    {
                        options.Delay = TimeSpan.FromSeconds(10);
                        options.Predicate = (check) => check.Tags.Contains("liveness");
                    });

                    services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();

                    services.AddHttpClient(string.Empty, client =>
                    {
                    });
                    services.AddDbContext<ProcesManagerDbContext>(opt =>
                        opt.UseSqlServer(config.GetConnectionString("ProcessManagerDbConnectionString")));

                    services.AddFeatureManagement();
                });
    }
}
