using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class UpdateProcessStatusDomainSucceeded : INotification
    {
        public UpdateProcessStatusDomainSucceeded(WorkflowRun workflowRun)
        {
            WorkflowRun = workflowRun;
        }

        public WorkflowRun WorkflowRun { get; }
    }
}
