using System;

namespace ProcessManager.Domain.Models.Reporting
{
    public class WorkflowRunReport
    {
        public Guid OperationId { get; set; }
        public string WorkflowRunName { get; set; }
        public string WorkflowRunId { get; set; }
        public string Status { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }
    }
}
