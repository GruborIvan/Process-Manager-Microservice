using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class StartLogicAppDomainSucceeded : INotification
    {
        public StartLogicAppDomainSucceeded(OutboxMessage outboxMessage)
        {
            OutboxMessage = outboxMessage;
        }

        public OutboxMessage OutboxMessage { get; }
    }
}
