using System;
using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class StartActivityDomainSucceeded : INotification
    {
        public StartActivityDomainSucceeded(Activity activity)
        {
            Activity = activity;
        }

        public Activity Activity { get; }
    }
}
