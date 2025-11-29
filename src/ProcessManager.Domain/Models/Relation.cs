using System;
using System.Collections.Generic;

namespace ProcessManager.Domain.Models
{
    public class Relation
    {
        public Guid EntityId { get; set; }
        public string EntityType { get; set; }

        public IList<WorkflowRun> WorkflowRuns { get; set; }
    }
}
