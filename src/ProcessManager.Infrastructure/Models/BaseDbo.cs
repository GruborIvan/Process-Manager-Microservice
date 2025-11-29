using System;

namespace ProcessManager.Infrastructure.Models
{
    public abstract class BaseDbo
    {
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }
    }
}
