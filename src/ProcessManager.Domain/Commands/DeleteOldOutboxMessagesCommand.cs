using System;

namespace ProcessManager.Domain.Commands
{
    public class DeleteOldOutboxMessagesCommand : ICommand
    {
        public DeleteOldOutboxMessagesCommand(int olderThanXDays)
        {
            CommandId = Guid.NewGuid();
            OlderThanXDays = olderThanXDays;
        }

        public Guid CommandId { get; }

        public int OlderThanXDays { get; }
    }
}
