using MediatR;
using ProcessManager.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Commands
{
    public class DeleteOldOutboxMessagesCommandHandler : ICommandHandler<DeleteOldOutboxMessagesCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteOldOutboxMessagesCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteOldOutboxMessagesCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.OutboxRepository.DeleteRangeOlderThanAsync(request.OlderThanXDays, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
