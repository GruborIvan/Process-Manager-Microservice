using System;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.Events
{
    public class StartProcessFailed : AbstractProcessEvent
    {
        public StartProcessFailed(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid operationId,
            string processName,
            string message,
            ErrorData error) : base(correlationId, requestId, commandId, operationId)
        {
            ProcessName = processName;
            Message = message;
            Error = error;
        }

        public string ProcessName { get; }
        public string Message { get; }
        public ErrorData Error { get; }
    }
}
