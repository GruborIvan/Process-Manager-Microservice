using System;

namespace ProcessManager.Infrastructure.Models
{
    public class WorkflowRelationDbo : BaseDbo
    {
        public Guid OperationId { get; set; }
        public WorkflowRunDbo WorkflowRun { get; set; }
        public Guid EntityId { get; set; }
        public RelationDbo Relation { get; set;}
    }
}
