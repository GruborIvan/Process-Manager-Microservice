using ProcessManager.Domain.Models;
using System;

namespace ProcessManager.Domain.Events
{
    public class InsertWorkflowRunFailed : AbstractProcessEvent
    {
        public InsertWorkflowRunFailed(
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
