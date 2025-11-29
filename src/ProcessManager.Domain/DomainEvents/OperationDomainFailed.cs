using System;
using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class OperationDomainFailed : INotification
    {
        public OperationDomainFailed(Guid operationId, ErrorData error, string resource)
        {
            OperationId = operationId;
            Error = error;
            Resource = resource;
        }

        public Guid OperationId { get; }
        public ErrorData Error { get; }
        public string Resource { get; }
    }
}
