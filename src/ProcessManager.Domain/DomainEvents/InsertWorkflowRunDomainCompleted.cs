using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class InsertWorkflowRunDomainCompleted : INotification
    {
        public InsertWorkflowRunDomainCompleted(UnorchestratedRun unorchestratedRun)
        {
            UnorchestratedRun = unorchestratedRun;
        }

        public UnorchestratedRun UnorchestratedRun { get; }
    }
}
