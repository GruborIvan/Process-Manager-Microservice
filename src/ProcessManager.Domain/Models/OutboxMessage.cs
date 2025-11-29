using System;
using ProcessManager.Domain.DomainEvents;

namespace ProcessManager.Domain.Models
{
    public class OutboxMessage : Entity
    {
        public Guid OutboxMessageId { get; set; }
        public Guid MessageId { get; set; }
        public DateTime CreatedDate { get; set; }
        public OutboxMessageType Type { get; set; }
        public string Data { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public DateTime? NextRetryDate { get; set; }
        public int? RetryAttempt { get; set; }

        public void ProcessMessage()
        {
            ProcessedDate = DateTime.UtcNow;
            if (Type == OutboxMessageType.LogicApp)
            {
                AddDomainEvent(new StartLogicAppDomainSucceeded(this));
            }
        }
    }

    public enum OutboxMessageType
    {
        EventGrid = 0,
        LogicApp = 1,
        EventHub = 2
    }
}
