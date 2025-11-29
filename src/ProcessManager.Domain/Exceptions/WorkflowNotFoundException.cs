using System;

namespace ProcessManager.Domain.Exceptions
{
    public class WorkflowNotFoundException : Exception
    {
        public WorkflowNotFoundException() : base("Workflow not found.")
        {

        }

        public WorkflowNotFoundException(Guid operationId) : base($"Workflow with {nameof(operationId)}: {operationId} not found.")
        {

        }
    }
}
