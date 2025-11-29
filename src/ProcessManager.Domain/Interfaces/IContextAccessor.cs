using System;
using System.Collections.Generic;

namespace ProcessManager.Domain.Interfaces
{
    public interface IContextAccessor
    {
        public Guid GetCommandId();
        public Guid GetRequestId();
        public Guid GetCorrelationId();
        public void CheckIfCommandIdAndRequestIdExists();
        public Dictionary<string, string> GetCurrentMessageHeaders();
    }
}
