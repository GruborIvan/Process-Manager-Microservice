using System;

namespace ProcessManager.Domain.Events
{
    public class AbstractProcessEvent
    {
        public AbstractProcessEvent(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid operationId)
        {
            CorrelationId = correlationId;
            RequestId = requestId;
            CommandId = commandId;
            OperationId = operationId;
            CreatedDate = DateTime.UtcNow;
        }

        public Guid CorrelationId { get; }
        public Guid RequestId { get; }
        public Guid CommandId { get; }
        public Guid OperationId { get; }
        public DateTime CreatedDate { get; }
    }
}
