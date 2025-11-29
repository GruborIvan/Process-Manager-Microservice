using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IEventNotificationService
    {
        Task SendAsync(object @event, string subject);
    }
}
