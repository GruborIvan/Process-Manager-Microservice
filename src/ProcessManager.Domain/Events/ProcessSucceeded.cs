using System;

namespace ProcessManager.Domain.Events
{
    public class ProcessSucceeded : AbstractProcessEvent
    {
        public ProcessSucceeded(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid operationId,
            string resource) : base(correlationId, requestId, commandId, operationId)
        {
            Resource = resource;
        }

        public string Resource { get; }
    }
}
