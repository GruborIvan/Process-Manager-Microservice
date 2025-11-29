using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Domain.Commands;

namespace ProcessManager.BackgroundWorker.SendEvents
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
            _logger.LogInformation("SendEvents Worker running at: {time}", DateTimeOffset.Now);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                        await mediator.Send(new SendEventsCommand(), stoppingToken);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("SendEvents Worker Error: {0}, Inner Exception: {1}", e.Message, e.InnerException);
                }

                await Task.Delay(_configuration.GetValue<int>("ProcessManagerConfiguration:SendEventsDelayInSec") * 1000, stoppingToken);
            }
            _logger.LogInformation("Worker shutting down at: {time}", DateTimeOffset.Now);

        }
    }
}
