using System;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IEventStreamingService
    {
        Task SendEvent(string @event, string name, Guid commandId);
    }
}
