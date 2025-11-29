using System;

namespace ProcessManager.Domain.Events
{
    public class StartActivitySucceeded : AbstractActivityEvent
    {
        public StartActivitySucceeded(
            Guid correlationId, 
            Guid requestId,
            Guid commandId,
            Guid operationId,
            Guid activityId,
            string name,
            string status,
            DateTime startDate,
            string uri
            ) : base(correlationId, requestId, commandId, activityId)
        {
            OperationId = operationId;
            Name = name;
            Status = status;
            StartDate = startDate;
            URI = uri;
        }

        public Guid OperationId { get; }
        public string Name { get; }
        public string Status { get; }
        public DateTime StartDate { get; }
        public string? URI { get; }
    }
}
