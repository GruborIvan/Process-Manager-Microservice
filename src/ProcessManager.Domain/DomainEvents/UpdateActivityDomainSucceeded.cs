using MediatR;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class UpdateActivityDomainSucceeded : INotification
    {
        public UpdateActivityDomainSucceeded(Activity activity)
        {
            Activity = activity;
        }

        public Activity Activity { get; }
    }
}
