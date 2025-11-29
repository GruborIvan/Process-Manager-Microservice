using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class EndActivityDomainSucceeded : INotification
    {
        public EndActivityDomainSucceeded(Activity activity)
        {
            Activity = activity;
        }

        public Activity Activity { get; }
    }
}
