using Microsoft.EntityFrameworkCore;
using ProcessManager.Domain.Exceptions;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Infrastructure.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;

namespace ProcessManager.Infrastructure.Repository
{
    public class ActivityRepository : IActivityRepository
    {
        private readonly ProcesManagerDbContext _dbContext;
        private readonly IMapper _mapper;

        public ActivityRepository(ProcesManagerDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper;
        }

        public async Task<Activity> AddAsync(Activity activity, CancellationToken ct = default)
        {
            var newActivity = (await _dbContext.Activities.AddAsync(_mapper.Map<ActivityDbo>(activity), ct)).Entity;
            return _mapper.Map<Activity>(newActivity);
        }

        public async Task<Activity> GetAsync(Guid activityId, CancellationToken ct = default)
        {
            var activity = await _dbContext
                .Activities
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ActivityId == activityId, ct)
                ?? throw new ActivityNotFoundException(activityId);

            return _mapper.Map<Activity>(activity);
        }

        public Activity Update(Activity activity)
        {
            var activityDbo = _dbContext
                                  .Activities
                                  .FirstOrDefault(x => x.ActivityId == activity.ActivityId)
                              ?? throw new ActivityNotFoundException(activity.ActivityId);

            activityDbo.Status = activity.Status;
            activityDbo.EndDate = activity.EndDate;
            activityDbo.URI = activity.URI;
            activityDbo.DomainEvents = activity.DomainEvents;

            return _mapper.Map<Activity>(activityDbo);
        }
    }
}
