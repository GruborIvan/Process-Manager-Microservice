using System;

namespace ProcessManager.Domain.Exceptions
{
    [Serializable]
    public class MissingCommandIdException : Exception
    {
        public MissingCommandIdException() : base("Missing x-command-id in headers")
        {

        }
    }
}
