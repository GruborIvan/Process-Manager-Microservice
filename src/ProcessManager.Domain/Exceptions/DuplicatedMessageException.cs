using System;

namespace ProcessManager.Domain.Exceptions
{
    [Serializable]
    public class DuplicatedMessageException : Exception
    {
        public DuplicatedMessageException(string messageType, string messageId)
            : base($"Message {messageType} with Id {messageId} already exists.")
        {

        }
    }
}
