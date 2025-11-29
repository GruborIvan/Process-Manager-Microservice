using FluentValidation;
using ProcessManager.Domain.Commands;

namespace ProcessManager.Domain.Validators
{
    public class StartActivityCommandValidator : Validator<StartActivityCommand>
    {
        public StartActivityCommandValidator()
        {
            RuleFor(x => x.ActivityId).NotEmpty();
            RuleFor(x => x.CommandId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.OperationId).NotEmpty();
            RuleFor(x => x.StartDate).NotEmpty();
        }
    }
}
