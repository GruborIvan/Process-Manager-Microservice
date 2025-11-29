using System;

namespace ProcessManager.Domain.Commands
{
    public class SendEventsCommand : ICommand
    {
        public SendEventsCommand()
        {
            CommandId = Guid.NewGuid();
        }

        public Guid CommandId { get; }
    }
}
