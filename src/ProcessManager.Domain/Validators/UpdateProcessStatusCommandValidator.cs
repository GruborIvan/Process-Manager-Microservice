using FluentValidation;
using ProcessManager.Domain.Commands;

namespace ProcessManager.Domain.Validators
{
    public class UpdateProcessStatusCommandValidator : Validator<UpdateProcessStatusCommand>
    {
        public UpdateProcessStatusCommandValidator()
        {
            RuleFor(x => x.CommandId).NotEmpty();
            RuleFor(x => x.OperationId).NotEmpty();
            RuleFor(x => x.Status).NotEmpty();
            RuleFor(x => x.EndDate).NotEmpty();
        }
    }
}
