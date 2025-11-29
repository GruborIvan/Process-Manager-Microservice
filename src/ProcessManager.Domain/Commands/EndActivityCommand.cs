using ProcessManager.Domain.Models;
using System;

namespace ProcessManager.Domain.Commands
{
    public class EndActivityCommand : ICommand<Activity>
    {
        public EndActivityCommand(
            Guid activityId,
            string status,
            DateTime endDate,
            string? uri)
        {
            CommandId = Guid.NewGuid();
            ActivityId = activityId;
            Status = status;
            EndDate = endDate;
            URI = uri;
        }

        public Guid CommandId { get; }
        public Guid ActivityId { get; }
        public string Status { get; }
        public DateTime EndDate { get; }
        public string? URI { get; }
    }
}
