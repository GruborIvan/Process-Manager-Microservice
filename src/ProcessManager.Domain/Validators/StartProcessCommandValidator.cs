using FluentValidation;
using ProcessManager.Domain.Commands;

namespace ProcessManager.Domain.Validators
{
    public class StartProcessCommandValidator : Validator<StartProcessCommand>
    {
        public StartProcessCommandValidator()
        {
            RuleFor(x => x.CommandId).NotEmpty();
            RuleFor(x => x.ProcessKey).NotEmpty();
            RuleFor(x => x.ProcessName).NotEmpty();
            RuleFor(x => x.StartMessage).NotNull();
        }
    }
}
