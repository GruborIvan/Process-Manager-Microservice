using System;
using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class UpdateActivityDomainFailed : INotification
    {
        public UpdateActivityDomainFailed(Guid correlationId, Guid activityId, string status, string? uri, ErrorData error)
        {
            CorrelationId = correlationId;
            ActivityId = activityId;
            Status = status;
            URI = uri;
            Error = error;
        }

        public Guid CorrelationId { get; }
        public Guid ActivityId { get; }
        public string Status { get; }
        public string? URI { get; }
        public ErrorData Error { get; }
    }
}
