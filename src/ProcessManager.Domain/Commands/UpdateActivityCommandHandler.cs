using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Domain.Validators;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Commands
{
    public class UpdateActivityCommandHandler : ICommandHandler<UpdateActivityCommand, Activity>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpdateActivityCommandValidator _validator;

        public UpdateActivityCommandHandler(
            IUnitOfWork unitOfWork,
            UpdateActivityCommandValidator validator
        )
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<Activity> Handle(UpdateActivityCommand request, CancellationToken cancellationToken)
        {
            _validator.ValidateAndThrow(request);
            var existingActivity = await _unitOfWork.ActivityRepository.GetAsync(request.ActivityId, cancellationToken);
            existingActivity.UpdateActivity(request.Status, request.URI);

            var activity = _unitOfWork.ActivityRepository.Update(existingActivity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return activity;
        }
    }
}
