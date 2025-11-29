using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class StartLogicAppDomainFailed : INotification
    {
        public StartLogicAppDomainFailed(OutboxMessage outboxMessage, string message, ErrorData error)
        {
            OutboxMessage = outboxMessage;
            Message = message;
            Error = error;
        }

        public OutboxMessage OutboxMessage { get; }
        public string Message { get; }
        public ErrorData Error { get; }
    }
}
