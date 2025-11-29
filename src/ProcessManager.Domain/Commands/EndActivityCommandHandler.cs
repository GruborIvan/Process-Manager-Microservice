using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Domain.Validators;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Commands
{
    public class EndActivityCommandHandler : ICommandHandler<EndActivityCommand, Activity>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly EndActivityCommandValidator _validator;

        public EndActivityCommandHandler(
            IUnitOfWork unitOfWork,
            EndActivityCommandValidator validator
            )
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<Activity> Handle(EndActivityCommand request, CancellationToken cancellationToken)
        {
            _validator.ValidateAndThrow(request);
            var existingActivity = await _unitOfWork.ActivityRepository.GetAsync(request.ActivityId, cancellationToken);
            existingActivity.EndActivity(request.Status, request.URI, request.EndDate);

            var activity = _unitOfWork.ActivityRepository.Update(existingActivity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return activity;
        }
    }
}
