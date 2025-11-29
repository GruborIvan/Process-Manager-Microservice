using System;
using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class StartProcessDomainFailed : INotification
    {
        public StartProcessDomainFailed(Guid correlationId, Guid operationId, string processName, string message, ErrorData error)
        {
            CorrelationId = correlationId;
            OperationId = operationId;
            ProcessName = processName;
            Message = message;
            Error = error;
        }

        public Guid CorrelationId { get; }
        public Guid OperationId { get; }
        public string ProcessName { get; }
        public string Message { get; }
        public ErrorData Error { get; }
    }
}
