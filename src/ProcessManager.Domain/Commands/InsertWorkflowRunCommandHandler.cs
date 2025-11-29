using MediatR;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Domain.Validators;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Commands
{
    public class InsertWorkflowRunCommandHandler : ICommandHandler<InsertWorkflowRunCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly InsertWorkflowRunCommandValidator _validator;

        public InsertWorkflowRunCommandHandler(IUnitOfWork unitOfWork, InsertWorkflowRunCommandValidator validator)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<Unit> Handle(InsertWorkflowRunCommand request, CancellationToken cancellationToken)
        {
            _validator.ValidateAndThrow(request);

            foreach (var entity in request.EntityIds)
            {
                var newUnorchestratedRun = new UnorchestratedRun(
                    request.UnorchestratedRunId,
                    request.OperationId,
                    Guid.Parse(entity),
                    request.WorkflowRunName,
                    request.WorkflowRunId
                );

                await _unitOfWork.UnorchestratedRepository.AddAsync(newUnorchestratedRun, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
