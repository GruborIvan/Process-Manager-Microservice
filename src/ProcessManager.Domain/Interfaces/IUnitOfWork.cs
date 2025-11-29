using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        IActivityRepository ActivityRepository { get; }
        IOutboxRepository OutboxRepository { get; }
        IReportingRepository ReportingRepository { get; }
        IWorkflowRepository WorkflowRepository { get; }
        IUnorchestratedRepository UnorchestratedRepository { get; }
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
