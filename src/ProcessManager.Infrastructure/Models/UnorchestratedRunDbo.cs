using ProcessManager.Domain.Models;
using System;

namespace ProcessManager.Infrastructure.Models
{
    public class UnorchestratedRunDbo : Entity
    {
        public Guid UnorchestratedRunId { get; set; }
        public Guid OperationId { get; set; }
        public Guid EntityId { get; set; }
        public string WorkflowRunName { get; set; }
        public string WorkflowRunId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ChangedDate { get; set; }
    }
}