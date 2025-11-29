using System;
using System.Collections.Generic;

namespace ProcessManager.Infrastructure.Models
{
    public class RelationDbo
    {
        public Guid EntityId { get; set; }
        public string EntityType { get; set; }

        public ICollection<WorkflowRelationDbo> WorkflowRelations { get; set; }
    }
}
