using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Domain.Commands;

namespace ProcessManager.BackgroundWorker.StartLogicApps
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StartLogicApps Worker running at: {time}", DateTimeOffset.Now);

            DateTime timeToRemoveOldOutboxMessages = DateTime.Now;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                        await mediator.Send(new StartLogicAppsCommand(), stoppingToken);

                        if (timeToRemoveOldOutboxMessages < DateTime.Now)
                        {
                            await mediator.Send(new DeleteOldOutboxMessagesCommand(_configuration.GetValue<int>("ProcessManagerConfiguration:OutboxMessagesRetentionDays")), stoppingToken);
                            timeToRemoveOldOutboxMessages = DateTime.Now.AddDays(1);
                            _logger.LogInformation("Worker removed old messages from OutboxMessages table at: {time}", DateTimeOffset.Now);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("StartLogicApps Worker Error: {0}, Inner Exception: {1}", e.Message, e.InnerException);
                }

                await Task.Delay(_configuration.GetValue<int>("ProcessManagerConfiguration:StartLogicAppsDelayInSeconds") * 1000, stoppingToken);
            }
            _logger.LogInformation("Worker shutting down at: {time}", DateTimeOffset.Now);

        }
    }
}
