using System;
using System.Collections.Generic;

namespace ProcessManager.Domain.Commands
{
    public class InsertWorkflowRunCommand : ICommand
    {
        public InsertWorkflowRunCommand(Guid unorchestratedRunId, Guid operationId, string workflowRunName, string workflowRunId, IEnumerable<string> entityIds)
        {
            CommandId = Guid.NewGuid();
            UnorchestratedRunId = unorchestratedRunId;
            OperationId = operationId;
            WorkflowRunName = workflowRunName;
            WorkflowRunId = workflowRunId;
            EntityIds = entityIds;
        }

        public Guid CommandId { get; }
        public Guid UnorchestratedRunId { get; set; }
        public Guid OperationId { get; }
        public string WorkflowRunName { get; }
        public string WorkflowRunId { get; }
        public IEnumerable<string> EntityIds { get; }
    }
}