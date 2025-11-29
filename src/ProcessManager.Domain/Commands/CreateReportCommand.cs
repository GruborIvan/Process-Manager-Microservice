using System;
using System.Collections.Generic;

namespace ProcessManager.Domain.Commands
{
    public class CreateReportCommand : ICommand
    {
        public CreateReportCommand(
            Guid correlationId,
            IEnumerable<string> dboEntities,
            DateTime? fromDatetime,
            DateTime? toDatetime
            )
        {
            CommandId = Guid.NewGuid();
            CorrelationId = correlationId;
            DboEntities = dboEntities;
            FromDatetime = fromDatetime;
            ToDatetime = toDatetime;
        }

        public Guid CommandId { get; }
        public Guid CorrelationId { get; }
        public IEnumerable<string> DboEntities { get; }
        public DateTime? FromDatetime { get; }
        public DateTime? ToDatetime { get; }
    }
}
