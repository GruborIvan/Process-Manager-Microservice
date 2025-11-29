using ProcessManager.Domain.Models;
using System;

namespace ProcessManager.Domain.Events
{
    public class InsertWorkflowRunSucceeded : AbstractProcessEvent
    {
        public InsertWorkflowRunSucceeded(
            Guid correlationId,
            Guid requestId,
            Guid commandId,
            Guid operationId,
            Guid unorchestratedRunId,
            Guid entityId,
            string workflowRunName,
            string workflowRunId) : base(correlationId, requestId, commandId, operationId)
        {
            UnorchestratedRunId = unorchestratedRunId;
            EntityId = entityId;
            WorkflowRunName = workflowRunName;
            WorkflowRunId = workflowRunId;
        }

        public Guid UnorchestratedRunId { get; set; }
        public Guid EntityId { get; set; }
        public string WorkflowRunName { get; set; }
        public string WorkflowRunId { get; set; }
    }
}
