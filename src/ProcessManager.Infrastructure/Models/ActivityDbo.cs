using System;
using ProcessManager.Domain.Models;

namespace ProcessManager.Infrastructure.Models
{
    public class ActivityDbo : Entity
    {
        public Guid ActivityId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string URI { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public Guid OperationId { get; set; }
        public WorkflowRunDbo WorkflowRun { get; set; }
    }
}
