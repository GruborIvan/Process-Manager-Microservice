using System;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.Events
{
    public class UpdateActivityFailed : AbstractActivityEvent
    {
        public UpdateActivityFailed(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid activityId,
            string status,
            string uri,
            ErrorData error) : base(correlationId, requestId, commandId, activityId)
        {
            Status = status;
            URI = uri;
            Error = error;
        }

        public string Status { get; }
        public string URI { get; }
        public ErrorData Error { get; }
    }
}
