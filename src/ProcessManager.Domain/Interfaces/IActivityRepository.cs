using ProcessManager.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IActivityRepository
    {
        public Task<Activity> GetAsync(Guid activityId, CancellationToken ct = default);
        public Task<Activity> AddAsync(Activity activity, CancellationToken ct = default);
        public Activity Update(Activity activity);
    }
}
