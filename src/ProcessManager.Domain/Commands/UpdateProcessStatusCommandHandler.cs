using System.Linq;
using MediatR;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Validators;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Commands
{
    public class UpdateProcessStatusCommandHandler : ICommandHandler<UpdateProcessStatusCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpdateProcessStatusCommandValidator _validator;

        public UpdateProcessStatusCommandHandler(
            IUnitOfWork unitOfWork,
            UpdateProcessStatusCommandValidator validator
        )
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<Unit> Handle(UpdateProcessStatusCommand request, CancellationToken cancellationToken)
        {
            _validator.ValidateAndThrow(request);
            var existingWorkflow = await _unitOfWork.WorkflowRepository.GetAsync(request.OperationId, cancellationToken);
            existingWorkflow.UpdateWorkflowRun(request.Status, request.EndDate, request.Errors?.FirstOrDefault(), request.Resource);

            _unitOfWork.WorkflowRepository.Update(existingWorkflow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
