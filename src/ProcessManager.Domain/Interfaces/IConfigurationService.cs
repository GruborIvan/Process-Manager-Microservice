using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IConfigurationService
    {
        public Task<string> GetConfigurationAsync(string processId);
    }
}
