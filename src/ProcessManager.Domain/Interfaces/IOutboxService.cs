using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IOutboxService
    {
        Task SendEventsAsync(CancellationToken ct = default);
        Task StartLogicAppsAsync(CancellationToken ct = default);
    }
}
