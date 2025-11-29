using System;

namespace ProcessManager.Domain.Commands
{
    public class StartLogicAppsCommand : ICommand
    {
        public StartLogicAppsCommand()
        {
            CommandId = Guid.NewGuid();
        }

        public Guid CommandId { get; }
    }
}
