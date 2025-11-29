using System;

namespace ProcessManager.Domain.Events
{
    public class EndActivitySucceeded : AbstractActivityEvent
    {
        public EndActivitySucceeded(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid activityId,
            string name,
            string status,
            DateTime? endDate,
            string? uri) 
            : base(correlationId, requestId, commandId, activityId)
        {
            Name = name;
            Status = status;
            EndDate = endDate;
            URI = uri;
        }

        public string Name { get; }
        public string Status { get; }
        public DateTime? EndDate { get; }
        public string? URI { get; }
    }
}
