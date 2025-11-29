using System;

namespace ProcessManager.Domain.Events
{
    public class StartProcessSucceeded : AbstractProcessEvent
    {
        public StartProcessSucceeded(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid operationId,
            string processName) : base(correlationId, requestId, commandId, operationId)
        {
            ProcessName = processName;
        }

        public string ProcessName { get; }
    }
}
