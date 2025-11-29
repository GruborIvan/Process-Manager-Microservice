using System;

namespace ProcessManager.Domain.Events
{
    public class AbstractActivityEvent
    {
        public AbstractActivityEvent(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid activityId)
        {
            CorrelationId = correlationId;
            RequestId = requestId;
            CommandId = commandId;
            CorrelationId = correlationId;
            ActivityId = activityId;
            CreatedDate = DateTime.UtcNow;
        }

        public Guid CorrelationId { get; }
        public Guid RequestId { get; }
        public Guid CommandId { get; }
        public Guid ActivityId { get; }
        public DateTime CreatedDate { get; }
    }
}