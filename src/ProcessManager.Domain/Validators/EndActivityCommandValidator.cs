using FluentValidation;
using ProcessManager.Domain.Commands;

namespace ProcessManager.Domain.Validators
{
    public class EndActivityCommandValidator : Validator<EndActivityCommand>
    {
        public EndActivityCommandValidator()
        {
            RuleFor(x => x.CommandId).NotEmpty();
            RuleFor(x => x.ActivityId).NotEmpty();
            RuleFor(x => x.Status).NotEmpty();
            RuleFor(x => x.EndDate).NotEmpty();
        }
    }
}
