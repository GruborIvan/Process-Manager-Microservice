using ProcessManager.Domain.Models;
using System;

namespace ProcessManager.Domain.Commands
{
    public class StartActivityCommand : ICommand<Activity>
    {
        public StartActivityCommand(
            Guid operationId,
            Guid activityId,
            string name,
            DateTime startDate,
            string uri)
        {
            CommandId = Guid.NewGuid();
            OperationId = operationId;
            ActivityId = activityId;
            Name = name;
            StartDate = startDate;
            URI = uri;
        }

        public Guid CommandId { get; }
        public Guid OperationId { get; }
        public Guid ActivityId { get; }
        public string Name { get; }
        public DateTime StartDate { get; }
        public string URI { get; }
    }
}
