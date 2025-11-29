using System;
using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class StartActivityDomainFailed : INotification
    {
        public StartActivityDomainFailed(Guid correlationId, Guid activityId, Guid operationId, string name, DateTime startDate, string? uri, ErrorData error)
        {
            CorrelationId = correlationId;
            ActivityId = activityId;
            OperationId = operationId;
            Name = name;
            StartDate = startDate;
            URI = uri;
            Error = error;
        }

        public Guid CorrelationId { get; }
        public Guid ActivityId { get; }
        public Guid OperationId { get; }
        public string Name { get; }
        public DateTime StartDate { get; }
        public string? URI { get; }
        public ErrorData Error { get; }
    }
}
