using System;

namespace ProcessManager.Domain.Models.Reporting
{
    public class ActivityReport
    {
        public Guid ActivityId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string URI { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid OperationId { get; set; }
    }
}
