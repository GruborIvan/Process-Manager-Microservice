using ProcessManager.Domain.DomainEvents;
using System;

namespace ProcessManager.Domain.Models
{
    public class UnorchestratedRun : Entity
    {
        public UnorchestratedRun()
        {

        }

        public UnorchestratedRun(Guid unorchestratedRunId, Guid operationId, Guid entityId, string workflowRunName, string workflowRunId)
        {
            UnorchestratedRunId = unorchestratedRunId;
            OperationId = operationId;
            EntityId = entityId;
            WorkflowRunName = workflowRunName;
            WorkflowRunId = workflowRunId;
            AddDomainEvent(new InsertWorkflowRunDomainCompleted(this));
        }

        public Guid UnorchestratedRunId { get; set; }
        public Guid OperationId { get; set; }
        public Guid EntityId { get; set; }
        public string WorkflowRunName { get; set; }
        public string WorkflowRunId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ChangedDate { get; set; }
    }
}
