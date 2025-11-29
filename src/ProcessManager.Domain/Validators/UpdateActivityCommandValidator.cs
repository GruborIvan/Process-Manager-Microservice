using FluentValidation;
using ProcessManager.Domain.Commands;

namespace ProcessManager.Domain.Validators
{
    public class UpdateActivityCommandValidator : Validator<UpdateActivityCommand>
    {
        public UpdateActivityCommandValidator()
        {
            RuleFor(x => x.CommandId).NotEmpty();
            RuleFor(x => x.ActivityId).NotEmpty();
            RuleFor(x => x.Status).NotEmpty();
            RuleFor(x => x.URI).NotEmpty();
        }
    }
}
