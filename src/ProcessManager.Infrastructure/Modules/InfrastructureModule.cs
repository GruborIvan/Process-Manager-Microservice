using System;
using Autofac;
using Azure.Core;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Validators;
using ProcessManager.Infrastructure.HealthChecks;
using ProcessManager.Infrastructure.Repository;
using ProcessManager.Infrastructure.Services;

namespace ProcessManager.Infrastructure.Modules
{
    public class InfrastructureModule : Module
    {
        private readonly IConfiguration _configuration;

        public InfrastructureModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void Load(ContainerBuilder builder)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            builder.RegisterAssemblyTypes(typeof(StartProcessCommandValidator).Assembly)
                        .AsClosedTypesOf(typeof(AbstractValidator<>));

            var settings = _configuration.GetSection("Settings");
            string sub = settings.GetSection("Subscription").Value;
            string rg = settings.GetSection("Resource").Value;

            builder.RegisterType<LogicAppProcessService>().As<IProcessService>()
                .WithParameter("subscription", sub)
                .WithParameter("resourceGroup", rg);

            builder.RegisterType<AzureTokenProvider>().As<IAzureTokenProvider>();
            builder.RegisterType<AzureServiceTokenProvider>().AsSelf();

            builder.RegisterType<LogicAppServiceHealthChecker>();
            builder.RegisterType<MemoryCache>().As<IMemoryCache>().SingleInstance();
            builder.RegisterType<CacheRepository>().As<ICacheRepository>();
            builder.RegisterType<EventHubService>().As<IEventStreamingService>();
            builder.RegisterType<WorkflowRepository>().As<IWorkflowRepository>();
            builder.RegisterType<ActivityRepository>().As<IActivityRepository>();
            builder.RegisterType<FeatureFlagService>().As<IFeatureFlagService>();
            builder.RegisterType<ReportingRepository>().As<IReportingRepository>();
            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>();
            builder.RegisterType<OutboxRepository>().As<IOutboxRepository>();
            builder.RegisterType<OutboxService>().As<IOutboxService>();
            builder.RegisterType<UnorchestratedRepository>().As<IUnorchestratedRepository>();

            builder.RegisterType<ConfigurationService>().As<IConfigurationService>()
                .WithParameter("environmentId",
                    _configuration.GetValue<string>("ProcessManagerConfiguration:EnvironmentId"))
                .WithParameter("configurationUri",
                    _configuration.GetValue<string>("ProcessManagerConfiguration:ConfigurationUri"))
                .WithParameter("configurationManagerBaseUrl",
                    _configuration.GetValue<string>("ProcessManagerConfiguration:ConfigurationManagerBaseUrl"))
                .WithParameter("apimBaseUrl", _configuration.GetValue<string>("General:ApiManagement:ApimBaseUrl"))
                .WithParameter("apiSubscriptionKey", _configuration.GetValue<string>("General:ApiManagement:TntInternalApiSubscriptionKey"))
                .WithParameter("authScope", _configuration.GetValue<string>("AuthorizationConfiguration:AuthScope"));

            builder.RegisterType<EventGridService>()
               .WithParameter("client", new EventGridClient(new TopicCredentials(_configuration.GetValue<string>("ProcessManagerConfiguration:EventGridTopicKey"))))
               .WithParameter("topicEndpoint", _configuration.GetValue<string>("ProcessManagerConfiguration:EventGridTopicEndpoint"))
               .As<IEventNotificationService>();

            builder.RegisterType<ReportingService>()
                .WithParameter("fileSystemName", _configuration.GetSection("DwhReporting")["AzureDataLakeServiceFileSystemName"])
                .As<IReportingService>();

            RegisterEventHubProducerClient(builder);
            RegisterAzureBlobServiceClient(builder);
        }

        private void RegisterAzureBlobServiceClient(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var blobServiceEndpoint = _configuration.GetSection("DwhReporting")["AzureDataLakeServiceEndpoint"];

                TokenCredential tokenCredential = AzureCredentials.GetCliCredentials();

                return new BlobServiceClient(
                    new Uri(blobServiceEndpoint),
                    tokenCredential);
            });
        }

        private void RegisterEventHubProducerClient(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var config = c.Resolve<IConfiguration>();
                var connectionString = config.GetConnectionString("EventHubConnectionString");
                var eventHubName = config.GetValue<string>("ProcessManagerConfiguration:EventHubName");

                TokenCredential tokenCredential = AzureCredentials.GetCliCredentials();

                return new EventHubProducerClient(connectionString, eventHubName, tokenCredential);
            })
            .SingleInstance()
            .OnRelease(instance => instance.DisposeAsync().GetAwaiter().GetResult());
        }
    }
}
