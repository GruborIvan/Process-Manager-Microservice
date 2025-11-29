using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Domain.Validators;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Commands
{
    public class StartActivityCommandHandler : ICommandHandler<StartActivityCommand, Activity>
    {
        private const string _inProgressStatus = "in progress";
        private readonly IUnitOfWork _unitOfWork;
        private readonly StartActivityCommandValidator _validator;

        public StartActivityCommandHandler(
            IUnitOfWork unitOfWork,
            StartActivityCommandValidator validator
        )
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<Activity> Handle(StartActivityCommand request, CancellationToken cancellationToken)
        {
            _validator.ValidateAndThrow(request);
            var newActivity = new Activity(
                request.ActivityId,
                request.OperationId,
                request.Name,
                _inProgressStatus,
                request.URI,
                request.StartDate
            );

            var activity = await _unitOfWork.ActivityRepository.AddAsync(newActivity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return activity;
        }
    }
}
