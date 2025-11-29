using ProcessManager.Domain.Models;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IProcessManager
    {
        Task<Process> GetProcessWithInputParameters(string processKey);
        Task StartProcess(Process process);
    }
}
