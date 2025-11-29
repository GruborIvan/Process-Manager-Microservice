using System;

namespace ProcessManager.API.Models
{
    public class WorkflowRunDto
    {
        public string Status { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastActionDateTime { get; set; }
    }
}
