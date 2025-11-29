using System;

namespace ProcessManager.Domain.Exceptions
{
    [Serializable]
    public class MissingRequestIdException : Exception
    {
        public MissingRequestIdException() : base("Missing x-request-id in headers")
        {

        }
    }
}
