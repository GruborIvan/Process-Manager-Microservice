using System;
using System.Collections.Generic;
using FiveDegrees.Messages.Orchestrator;
using FiveDegrees.Messages.Orchestrator.Interfaces;

namespace ProcessManager.Tests.UnitTests.Domain
{
    internal class StartProcessMsgV3 : IStartProcessMsgV3
    {
        public StartProcessMsgV3(Guid operationId, string environmentShortName, IEnumerable<EntityRelation> entityRelations)
        {
            OperationId = operationId;
            EnvironmentShortName = environmentShortName;
            EntityRelations = entityRelations;
        }

        public string ProcessKey => "Test-ProcessKey";

        public string ProcessName => "Test-ProcessName";

        public Guid OperationId { get; }

        public string EnvironmentShortName { get; }

        public IEnumerable<EntityRelation> EntityRelations { get; }
    }
}
