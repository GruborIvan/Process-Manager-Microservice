using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IReportingService
    {
        Task StoreReportAsync(Guid correlationId, Dictionary<string, byte[]> files, CancellationToken ct = default);
        Task<Dictionary<string, byte[]>> GetReportingDataAsync(IEnumerable<string> dboEntities, DateTime? fromDate, DateTime? toDatetime, CancellationToken ct = default);
    }
}
