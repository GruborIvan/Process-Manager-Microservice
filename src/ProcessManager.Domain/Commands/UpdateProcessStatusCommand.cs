using System;
using System.Collections.Generic;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.Commands
{
    public class UpdateProcessStatusCommand : ICommand
    {
        public UpdateProcessStatusCommand(
            Guid operationId, 
            string status,
            DateTime endDate,
            IEnumerable<ErrorData> errors,
            string resource)
        {
            CommandId = Guid.NewGuid();
            OperationId = operationId;
            Status = status;
            EndDate = endDate;
            Errors = errors;
            Resource = resource;
        }

        public Guid CommandId { get; }
        public Guid OperationId { get; }
        public string Status { get; }
        public DateTime EndDate { get; }
        public IEnumerable<ErrorData> Errors { get; }
        public string Resource { get; }
    }
}
