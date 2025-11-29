using System;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.Events
{
    public class ProcessFailed : AbstractProcessEvent
    {
        public ProcessFailed(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid operationId,
            ErrorData error,
            string resource) : base(correlationId, requestId, commandId, operationId)
        {
            Error = error;
            Resource = resource;
        }

        public ErrorData Error { get; }
        public string Resource { get; }
    }
}
