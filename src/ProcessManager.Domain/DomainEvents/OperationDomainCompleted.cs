using System;
using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class OperationDomainCompleted : INotification
    {
        public OperationDomainCompleted(Guid operationId, ErrorData error, string resource, string status)
        {
            OperationId = operationId;
            Error = error;
            Resource = resource;
            Status = status;
        }

        public Guid OperationId { get; }
        public ErrorData Error { get; }
        public string Resource { get; }
        public string Status { get; }
    }
}
