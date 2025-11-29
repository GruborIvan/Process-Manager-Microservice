using System;
using ProcessManager.Domain.DomainEvents;

namespace ProcessManager.Domain.Models
{
    public class Activity : Entity
    {
        public Activity(Guid activityId, Guid operationId, string name, string status, string uri, DateTime startDate, DateTime? endDate = null, WorkflowRun workflowRun = null)
        {
            ActivityId = activityId;
            OperationId = operationId;
            Name = name;
            Status = status;
            URI = uri;
            StartDate = startDate;
            EndDate = endDate;
            WorkflowRun = workflowRun;
            AddDomainEvent(new StartActivityDomainSucceeded(this));
        }

        public void EndActivity(string status, string uri, DateTime? endDate)
        {
            Status = status;
            URI = uri;
            EndDate = endDate;
            AddDomainEvent(new EndActivityDomainSucceeded(this));
        }

        public void UpdateActivity(string status, string uri)
        {
            Status = status;
            URI = uri;
            AddDomainEvent(new UpdateActivityDomainSucceeded(this));
        }

        public Guid ActivityId { get; private set; }
        public Guid OperationId { get; private set; }
        public string Name { get; private set; }
        public string Status { get; private set; }
        public string URI { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public WorkflowRun WorkflowRun { get; private set; }
    }
}
