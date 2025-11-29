using MediatR;
using ProcessManager.Domain.Models;
using System;

namespace ProcessManager.Domain.DomainEvents
{
    public class InsertWorkflowRunDomainFailed : INotification
    {
        public InsertWorkflowRunDomainFailed(Guid operationId, ErrorData error)
        {
            OperationId = operationId;
            Error = error;
        }

        public Guid OperationId { get; }
        public ErrorData Error { get; }
    }
}
