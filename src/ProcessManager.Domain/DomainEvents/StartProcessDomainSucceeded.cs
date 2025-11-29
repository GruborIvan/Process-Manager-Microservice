using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class StartProcessDomainSucceeded : INotification
    {
        public StartProcessDomainSucceeded(WorkflowRun workflowRun)
        {
            WorkflowRun = workflowRun;
        }

        public WorkflowRun WorkflowRun { get; }
    }
}
