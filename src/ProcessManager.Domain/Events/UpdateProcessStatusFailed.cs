using System;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.Events
{
    public class UpdateProcessStatusFailed : AbstractProcessEvent
    {
        public UpdateProcessStatusFailed(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid operationId,
            ErrorData error) : base(correlationId, requestId, commandId, operationId)
        {
            Error = error;
        }

        public ErrorData Error { get; }
    }
}
