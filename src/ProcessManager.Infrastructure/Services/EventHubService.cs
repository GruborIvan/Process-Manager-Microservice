using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Newtonsoft.Json;
using ProcessManager.Domain.Interfaces;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManager.Infrastructure.Services
{
    public class EventHubService : IEventStreamingService
    {
        private readonly EventHubProducerClient _eventHub;

        public EventHubService(EventHubProducerClient eventHub)
        {
            _eventHub = eventHub;
        }

        public async Task SendEvent(string @event, string name, Guid commandId)
        {
            var taskEvent = new {
               CreateDate = DateTime.UtcNow,
               Id = Guid.NewGuid(),
               Name = name,
               EventType = @event,
               CorrelationId = commandId
            };
            var body = JsonConvert.SerializeObject(taskEvent);

            using (var batch = await _eventHub.CreateBatchAsync())
            {
                batch.TryAdd(new EventData(Encoding.UTF8.GetBytes(body)));
                await _eventHub.SendAsync(batch);
            }
        }
    }
}
