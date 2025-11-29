using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Infrastructure.Models;

namespace ProcessManager.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        public IActivityRepository ActivityRepository { get; }
        public IOutboxRepository OutboxRepository { get; }
        public IReportingRepository ReportingRepository { get; }
        public IWorkflowRepository WorkflowRepository { get; }
        public IUnorchestratedRepository UnorchestratedRepository { get; }
        private readonly ProcesManagerDbContext _dbContext;
        private readonly IMediator _mediator;

        public UnitOfWork(ProcesManagerDbContext dbContext, IActivityRepository activityRepository, IOutboxRepository outboxRepository, IReportingRepository reportingRepository, IWorkflowRepository workflowRepository, IUnorchestratedRepository unorchestratedRepository, IMediator mediator)
        {
            _dbContext = dbContext;
            ActivityRepository = activityRepository;
            OutboxRepository = outboxRepository;
            ReportingRepository = reportingRepository;
            WorkflowRepository = workflowRepository;
            UnorchestratedRepository = unorchestratedRepository;
            _mediator = mediator;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _mediator.DispatchDomainEventsAsync(_dbContext);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
