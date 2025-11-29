using System;

namespace ProcessManager.Domain.Events
{
    public class UpdateProcessStatusSucceeded : AbstractProcessEvent
    {
        public UpdateProcessStatusSucceeded(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid operationId) : base(correlationId, requestId, commandId, operationId)
        {

        }
    }
}
