using ProcessManager.Domain.Models;
using System;

namespace ProcessManager.Domain.Commands
{
    public class UpdateActivityCommand : ICommand<Activity>
    {
        public UpdateActivityCommand(
            Guid activityId, 
            string status, 
            string uri)
        {
            CommandId = Guid.NewGuid();
            ActivityId = activityId;
            Status = status;
            URI = uri;
        }
        public Guid CommandId { get; }
        public Guid ActivityId { get; }
        public string Status { get; }
        public string URI { get; }
    }
}
