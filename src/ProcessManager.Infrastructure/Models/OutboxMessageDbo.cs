using System;
using ProcessManager.Domain.Models;

namespace ProcessManager.Infrastructure.Models
{
    public class OutboxMessageDbo : Entity
    {
        public Guid OutboxMessageId { get; set; }
        public Guid MessageId { get; set; }
        public DateTime CreatedDate { get; set; }
        public OutboxMessageType Type { get; set; }
        public string Data { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public DateTime? NextRetryDate { get; set; }
        public int? RetryAttempt { get; set; }
    }
}
