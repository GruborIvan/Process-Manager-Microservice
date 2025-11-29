using System;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.Events
{
    public class StartActivityFailed : AbstractActivityEvent
    {
        public StartActivityFailed(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid operationId,
            Guid activityId,
            string name,
            DateTime startDate,
            string uri,
            ErrorData error) : base(correlationId, requestId, commandId, activityId)
        {
            OperationId = operationId;
            Name = name;
            StartDate = startDate;
            URI = uri;
            Error = error;
        }

        public Guid OperationId { get; }
        public string Name { get; }
        public DateTime StartDate { get; }
        public string? URI { get; }
        public ErrorData Error { get; }
    }
}
