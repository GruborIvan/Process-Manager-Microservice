using System;

namespace ProcessManager.Domain.Exceptions
{
    public class UnorchestratedRunNotFoundException : Exception
    {
        public UnorchestratedRunNotFoundException() : base("Unorchestrated run not found.")
        {

        }

        public UnorchestratedRunNotFoundException(Guid unorchestratedRunId) : base($"Unorchestrated run with {nameof(unorchestratedRunId)}: {unorchestratedRunId} not found.")
        {

        }
    }
}
