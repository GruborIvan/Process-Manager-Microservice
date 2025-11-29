using System;
using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class EndActivityDomainFailed : INotification
    {
        public EndActivityDomainFailed(Guid correlationId, Guid activityId, string status, DateTime endDate, string? uri, ErrorData error)
        {
            CorrelationId = correlationId;
            ActivityId = activityId;
            Status = status;
            EndDate = endDate;
            URI = uri;
            Error = error;
        }

        public Guid CorrelationId { get; }
        public Guid ActivityId { get; }
        public string Status { get; }
        public DateTime EndDate { get; }
        public string? URI { get; }
        public ErrorData Error { get; }
    }
}
