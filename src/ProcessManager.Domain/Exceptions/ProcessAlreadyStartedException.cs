using System;

namespace ProcessManager.Domain.Exceptions
{
    public class ProcessAlreadyStartedException : Exception
    {
        public ProcessAlreadyStartedException() : base("Process already started.")
        {

        }

        public ProcessAlreadyStartedException(Guid operationId) : base($"Process with {nameof(operationId)}: {operationId} already started.")
        {

        }
    }
}
