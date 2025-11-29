using ProcessManager.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IUnorchestratedRepository
    {
        public Task<UnorchestratedRun> GetAsync(Guid unorchestratedRunId, CancellationToken ct = default);
        public Task<UnorchestratedRun> AddAsync(UnorchestratedRun unorchestratedRun, CancellationToken ct = default);
    }
}
