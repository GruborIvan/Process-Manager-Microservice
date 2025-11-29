using System;
using ProcessManager.Domain.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ProcessManager.Domain.Interfaces
{
    public interface IProcessService
    {
        Task<Process> GetProcessWithMessageAsync(string processKey, string processName, object message, string environmentName);
        Task<string> StartProcessAsync(Process process, Dictionary<string, string> headers);
        Task<Guid> GetPrincipalIdAsync(string processName, string environmentName);
    }
}
