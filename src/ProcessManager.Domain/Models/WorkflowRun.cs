using System;
using System.Collections.Generic;
using ProcessManager.Domain.DomainEvents;

namespace ProcessManager.Domain.Models
{
    public class WorkflowRun : Entity
    {
        public WorkflowRun()
        {

        }

        public WorkflowRun(Guid operationId, string workflowRunName, string status, string createdBy, Process process)
        {
            OperationId = operationId;
            WorkflowRunName = workflowRunName;
            Status = status;
            CreatedBy = createdBy;
            Process = process;
            AddDomainEvent(new StartProcessDomainSucceeded(this));
        }

        public void UpdateWorkflowRun(string status, DateTime endDate, ErrorData error, string resource)
        {
            Status = status;
            EndDate = endDate;
            AddDomainEvent(new UpdateProcessStatusDomainSucceeded(this));
            AddDomainEvent(new OperationDomainCompleted(OperationId, error, resource, status));
        }

        public void UpdateWorkflowStatus(string status)
        {
            Status = status;
        }

        public Guid OperationId { get; private set; }
        public string WorkflowRunName { get; private set; }
        public string WorkflowRunId { get; set; }
        public string Status { get; private set; }
        public string CreatedBy { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public string ChangedBy { get; private set; }
        public DateTime ChangedDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public IList<Relation> Relations { get; private set; }
        public IList<Activity> Activities { get; private set; }
        public Process Process { get; private set; }
    }
}
