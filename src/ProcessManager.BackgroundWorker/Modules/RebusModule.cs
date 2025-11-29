using Autofac;
using Autofac.Features.Variance;
using Azure.Core;
using FiveDegrees.Audit.Rebus.Extensions;
using Microsoft.Extensions.Configuration;
using ProcessManager.BackgroundWorker.Extensions;
using ProcessManager.BackgroundWorker.Handlers;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Infrastructure.Providers;
using ProcessManager.Infrastructure.Services;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Serialization;
using Rebus.Serialization.Json;

namespace ProcessManager.BackgroundWorker.Modules
{
    public class RebusModule : Module
    {
        private readonly IConfiguration _configuration;
        public RebusModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterSource(new ContravariantRegistrationSource());

            builder.RegisterHandlersFromAssemblyOf<EndActivityMessageHandler>();

            TokenCredential tokenCredential = AzureCredentials.GetCliCredentials();

            builder.RegisterRebus((configurer, context) => configurer
                    .Logging(l => l.Serilog())
                    .Transport(t => t.UseAzureServiceBus(
                        _configuration.GetConnectionString("ServiceBusConnectionString"),
                        _configuration.GetValue<string>("ProcessManagerConfiguration:ServiceBusQueueName"),
                        new AzureIdentityServiceBusCredentialAdapter(tokenCredential)))
                    .Serialization(s => s.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
                    .Options(o =>
                    {
                        o.SimpleRetryStrategy(secondLevelRetriesEnabled: true);
                        o.AddFdsAudit(_configuration.GetValue<bool>("ProcessManagerConfiguration:AuditEnabled"));
                        o.UseContainerScopeInitializerStep(context.Resolve<ILifetimeScope>());
                        o.EnableMessageDeDuplication();
                        o.UseFailFastChecker();
                        o.Decorate<ISerializer>(c => new MessageDeserializer(c.Get<ISerializer>()));
                    })
            ); 
        }
    }
}
