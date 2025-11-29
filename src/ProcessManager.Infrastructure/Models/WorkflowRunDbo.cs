using System;
using System.Collections.Generic;
using ProcessManager.Domain.Models;

namespace ProcessManager.Infrastructure.Models
{
    public class WorkflowRunDbo : Entity
    {
        public Guid OperationId { get; set; }
        public string WorkflowRunName { get; set; }
        public string Status { get; set; }
        public string WorkflowRunId { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }

        public ICollection<ActivityDbo> Activities { get; set; }
        public ICollection<WorkflowRelationDbo> WorkflowRelations { get; set; }
    }
}
