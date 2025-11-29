using MediatR;
using ProcessManager.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Commands
{
    public class StartLogicAppsCommandHandler : ICommandHandler<StartLogicAppsCommand>
    {
        private readonly IOutboxService _outboxService;

        public StartLogicAppsCommandHandler(IOutboxService outboxService)
        {
            _outboxService = outboxService;
        }

        public async Task<Unit> Handle(StartLogicAppsCommand request, CancellationToken cancellationToken)
        {
            await _outboxService.StartLogicAppsAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
