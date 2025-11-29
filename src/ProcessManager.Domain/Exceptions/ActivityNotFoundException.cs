using System;

namespace ProcessManager.Domain.Exceptions
{
    public class ActivityNotFoundException : Exception
    {
        public ActivityNotFoundException() : base("Activity not found.")
        {

        }

        public ActivityNotFoundException(Guid activityId) : base($"Activity with {nameof(activityId)}: {activityId} not found.")
        {

        }
    }
}
