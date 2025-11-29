using MediatR;
using ProcessManager.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class StartLogicAppDomainSucceededHandler : INotificationHandler<StartLogicAppDomainSucceeded>
    {
        private readonly IUnitOfWork _unitOfWork;

        public StartLogicAppDomainSucceededHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(StartLogicAppDomainSucceeded notification, CancellationToken cancellationToken)
        {
            var jsonEvent = JObject.Parse(notification.OutboxMessage.Data);
            jsonEvent.Remove("process");
            await _unitOfWork.OutboxRepository.AddAsync(notification.OutboxMessage.MessageId, OutboxMessageType.EventGrid, jsonEvent.ToString(), cancellationToken);
        }
    }
}
