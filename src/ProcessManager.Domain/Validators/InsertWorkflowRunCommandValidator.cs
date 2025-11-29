using FluentValidation;
using ProcessManager.Domain.Commands;

namespace ProcessManager.Domain.Validators
{
    public class InsertWorkflowRunCommandValidator : Validator<InsertWorkflowRunCommand>
    {
        public InsertWorkflowRunCommandValidator()
        {
            RuleFor(x => x.CommandId).NotEmpty();
            RuleFor(x => x.UnorchestratedRunId).NotEmpty();
            RuleFor(x => x.EntityIds).NotEmpty();
            RuleFor(x => x.WorkflowRunName).NotEmpty();
            RuleFor(x => x.WorkflowRunId).NotEmpty();
        }
    }
}
