using ProcessManager.Domain.Models;
using System;
using System.Collections.Generic;

namespace ProcessManager.Domain.Commands
{
    public class StartProcessCommand : ICommand
    {
        public StartProcessCommand(
            string processKey, 
            string processName,
            Guid createdBy, 
            object startMessage,
            Guid operationId,
            IEnumerable<Relation> relations,
            string environmentShortName)
        {
            CommandId = Guid.NewGuid();
            ProcessKey = processKey;
            ProcessName = processName;
            CreatedBy = createdBy;
            StartMessage = startMessage;
            OperationId = operationId;
            Relations = relations;
            EnvironmentShortName = environmentShortName;
        }

        public Guid OperationId { get; set; }
        public Guid CommandId { get; }
        public string ProcessKey { get; }
        public string ProcessName { get; }
        public Guid CreatedBy { get; }
        public object StartMessage { get; }
        public string EnvironmentShortName { get; }
        public IEnumerable<Relation> Relations { get; }
    }
}
