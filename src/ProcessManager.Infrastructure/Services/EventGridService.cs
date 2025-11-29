using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json.Linq;
using ProcessManager.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessManager.Infrastructure.Services
{
    public class EventGridService : IEventNotificationService
    {
        private const string _version = "0.1";

        private readonly IEventGridClient _eventGridClient;
        private readonly string _topicHostname;

        public EventGridService(IEventGridClient client, string topicEndpoint)
        {
            _topicHostname = new Uri(topicEndpoint).Host;
            _eventGridClient = client;
        }

        public async Task SendAsync(object @event, string subject)
        {
            Validate(@event, subject);

            var events = CreateEventsList(@event, subject, _version);
            await _eventGridClient.PublishEventsAsync(_topicHostname, events);
        }

        private void Validate(object @event, string subject)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }
        }

        private IList<EventGridEvent> CreateEventsList(object @event, string subject, string version)
        {
            return new List<EventGridEvent>
            {
                new EventGridEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    EventType = @event.GetType().Name,
                    Data = JObject.FromObject(@event),
                    EventTime = DateTime.UtcNow,
                    Subject = subject,
                    DataVersion = version,
                    Topic = "processes"
                }
            };
        }
    }
}
