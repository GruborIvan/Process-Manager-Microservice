using System;
using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class UpdateProcessStatusDomainFailed : INotification
    {
        public UpdateProcessStatusDomainFailed(Guid correlationId, Guid operationId, ErrorData error)
        {
            CorrelationId = correlationId;
            OperationId = operationId;
            Error = error;
        }

        public Guid CorrelationId { get; }
        public Guid OperationId { get; }
        public ErrorData Error { get; }
    }
}
