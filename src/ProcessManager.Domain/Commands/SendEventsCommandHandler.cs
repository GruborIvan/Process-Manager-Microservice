using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ProcessManager.Domain.Interfaces;

namespace ProcessManager.Domain.Commands
{
    public class SendEventsCommandHandler : ICommandHandler<SendEventsCommand>
    {
        private readonly IOutboxService _outboxService;

        public SendEventsCommandHandler(IOutboxService outboxService)
        {
            _outboxService = outboxService;
        }

        public async Task<Unit> Handle(SendEventsCommand request, CancellationToken cancellationToken)
        {
            await _outboxService.SendEventsAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
