using System;

namespace ProcessManager.Domain.Models.Reporting
{
    public class WorkflowRelationReport
    {
        public Guid OperationId { get; set; }
        public Guid EntityId { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }
    }
}
