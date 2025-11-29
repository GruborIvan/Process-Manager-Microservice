using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FiveDegrees.Messages.Orchestrator.Interfaces;
using FiveDegrees.Messages.ProcessManager;
using FiveDegrees.Messages.ProcessManager.v2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Bus;

namespace ProcessManager.BackgroundWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IBus _bus;

        public Worker(ILogger<Worker> logger, IBus bus)
        {
            _logger = logger;
            _bus = bus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            await _bus.Subscribe<UpdateProcessStatusMsg>();
            await _bus.Subscribe<StartActivityMsg>();
            await _bus.Subscribe<EndActivityMsg>();
            await _bus.Subscribe<UpdateActivityMsg>();
            await _bus.Subscribe<UpdateProcessStatusMsgV2>();
            await _bus.Subscribe<StartActivityMsgV2>();
            await _bus.Subscribe<EndActivityMsgV2>();
            await _bus.Subscribe<UpdateActivityMsgV2>();
            await _bus.Subscribe<ReportingProcessManagerMsg>();
            await _bus.Subscribe<InsertWorkflowRunMsg>();

            int messagesPerStep = 10;
            var startMessageTypes = GetStartMessageTypes()
                .Concat(GetV2StartMessageTypes())
                .Concat(GetV3StartMessageTypes());
            
            for (int i = 0; i < startMessageTypes.Count(); i += messagesPerStep)
            {
                await Task.WhenAll(startMessageTypes.Skip(i).Take(messagesPerStep)
                    .Select(x => _bus.Subscribe(x)));
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation("Worker shutting down at: {time}", DateTimeOffset.Now);

            await _bus.Unsubscribe<UpdateProcessStatusMsg>();
            await _bus.Unsubscribe<StartActivityMsg>();
            await _bus.Unsubscribe<EndActivityMsg>();
            await _bus.Unsubscribe<UpdateActivityMsg>();
            await _bus.Unsubscribe<UpdateProcessStatusMsgV2>();
            await _bus.Unsubscribe<StartActivityMsgV2>();
            await _bus.Unsubscribe<EndActivityMsgV2>();
            await _bus.Unsubscribe<UpdateActivityMsgV2>();
            await _bus.Unsubscribe<ReportingProcessManagerMsg>();
            await _bus.Unsubscribe<InsertWorkflowRunMsg>();

            for (int i = 0; i < startMessageTypes.Count(); i += messagesPerStep)
            {
                await Task.WhenAll(startMessageTypes.Skip(i).Take(messagesPerStep)
                    .Select(x => _bus.Unsubscribe(x)));
            }
        }

        private IEnumerable<Type> GetStartMessageTypes()
        {
            var assembly = typeof(IStartProcessMsg).Assembly;
            var startMessageTypes = assembly.GetTypes()
                .Where(x => !x.IsInterface && !x.IsAbstract)
                .Where(x => x.GetInterfaces().Any(x => x == typeof(IStartProcessMsg)));
            return startMessageTypes;
        }

        private IEnumerable<Type> GetV2StartMessageTypes()
        {
            var assembly = typeof(IStartProcessMsgV2).Assembly;
            var startMessageTypes = assembly.GetTypes()
                .Where(x => !x.IsInterface && !x.IsAbstract)
                .Where(x => x.GetInterfaces().Any(x => x == typeof(IStartProcessMsgV2)));
            return startMessageTypes;
        }

        private IEnumerable<Type> GetV3StartMessageTypes()
        {
            var assembly = typeof(IStartProcessMsgV3).Assembly;
            var startMessageTypes = assembly.GetTypes()
                .Where(x => !x.IsInterface && !x.IsAbstract)
                .Where(x => x.GetInterfaces().Any(x => x == typeof(IStartProcessMsgV3)));
            return startMessageTypes;
        }
    }
}
