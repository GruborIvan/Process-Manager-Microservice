using ProcessManager.Domain.Models.Reporting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IReportingRepository
    {
        Task<IEnumerable<ActivityReport>> GetActivitiesAsync(DateTime? fromDate, DateTime? toDatetime, CancellationToken ct = default);
        Task<IEnumerable<RelationReport>> GetRelationsAsync(CancellationToken ct = default);
        Task<IEnumerable<WorkflowRelationReport>> GetWorkflowRelationsAsync(DateTime? fromDate, DateTime? toDatetime, CancellationToken ct = default);
        Task<IEnumerable<WorkflowRunReport>> GetWorkflowRunsAsync(DateTime? fromDate, DateTime? toDatetime, CancellationToken ct = default);
    }
}
