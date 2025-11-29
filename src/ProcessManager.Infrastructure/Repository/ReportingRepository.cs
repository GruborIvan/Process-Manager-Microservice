using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models.Reporting;
using ProcessManager.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Infrastructure.Repository
{
    public class ReportingRepository : IReportingRepository
    {
        private readonly ProcesManagerDbContext _dbContext;
        private readonly IMapper _mapper;

        public ReportingRepository(ProcesManagerDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper;
        }

        public async Task<IEnumerable<ActivityReport>> GetActivitiesAsync(DateTime? fromDate, DateTime? toDatetime, CancellationToken ct = default)
        {
            var activities = await _dbContext
                .Activities
                .Where(c => (c.EndDate ?? c.StartDate) >= (fromDate ?? DateTime.MinValue) && (c.EndDate ?? c.StartDate) <= (toDatetime ?? DateTime.MaxValue))
                .AsNoTracking()
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<ActivityReport>>(activities);
        }

        public async Task<IEnumerable<RelationReport>> GetRelationsAsync(CancellationToken ct = default)
        {
            var relations = await _dbContext
                .Relations
                .AsNoTracking()
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<RelationReport>>(relations);
        }

        public async Task<IEnumerable<WorkflowRelationReport>> GetWorkflowRelationsAsync(DateTime? fromDate, DateTime? toDatetime, CancellationToken ct = default)
        {
            var workflowRelations = await _dbContext
                .WorkflowRelations
                .Where(c => c.ChangedDate >= (fromDate ?? DateTime.MinValue) && c.ChangedDate <= (toDatetime ?? DateTime.MaxValue))
                .AsNoTracking()
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<WorkflowRelationReport>>(workflowRelations);
        }

        public async Task<IEnumerable<WorkflowRunReport>> GetWorkflowRunsAsync(DateTime? fromDate, DateTime? toDatetime, CancellationToken ct = default)
        {
            var workflowRuns = await _dbContext
                .WorkflowRuns
                .Where(c => c.ChangedDate >= (fromDate ?? DateTime.MinValue) && c.ChangedDate <= (toDatetime ?? DateTime.MaxValue))
                .AsNoTracking()
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<WorkflowRunReport>>(workflowRuns);
        }
    }
}
