using ProcessManager.Domain.Exceptions;
using ProcessManager.Domain.Interfaces;
using Rebus.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rebus.Retry.Simple;

namespace ProcessManager.BackgroundWorker.Helpers
{
    public class RebusContextAccessor : IContextAccessor
    {
        private const string _requestIdHeaderKey = "x-request-id";
        private const string _commandIdHeaderKey = "x-command-id";

        public Guid GetCommandId()
        {
            var commandIdValue = GetCurrentMessageHeaders()
                .FirstOrDefault(mc => mc.Key == _commandIdHeaderKey).Value;

            return (Guid.TryParse
                (commandIdValue, out var commandId))
                ? commandId
                : Guid.Empty;
        }

        public Guid GetRequestId()
        {
            var requestIdValue = GetCurrentMessageHeaders()
                .FirstOrDefault(mc => mc.Key == _requestIdHeaderKey).Value;

            return (Guid.TryParse
                (requestIdValue, out var requestId))
                ? requestId
                : Guid.Empty;
        }

        public Guid GetCorrelationId()
        {
            var messageBody =  MessageContext.Current.Message.Body;

            if (messageBody is IFailed<object> failed)
            {
                messageBody = failed.Message;
            }

            //Temporary fix, since the CorrelationId place will be in header
            PropertyInfo correlationProperty = messageBody
                .GetType()
                .GetProperty("CorrelationId");

            if (correlationProperty is { }) return (Guid)correlationProperty.GetValue(messageBody);

            var correlationId = MessageContext.Current.Message.Headers
                .FirstOrDefault(mc => mc.Key == _requestIdHeaderKey).Value;

            return (Guid.TryParse
                (correlationId, out var requestId))
                ? requestId
                : Guid.Empty;
        }

        public void CheckIfCommandIdAndRequestIdExists()
        {
            var headers = GetCurrentMessageHeaders();
            _ = headers.FirstOrDefault(x => x.Key.Equals(_commandIdHeaderKey, StringComparison.OrdinalIgnoreCase)).Value
                ?? throw new MissingCommandIdException();
            _ = headers.FirstOrDefault(x => x.Key.Equals(_requestIdHeaderKey, StringComparison.OrdinalIgnoreCase)).Value
                ?? throw new MissingRequestIdException();
        }

        public Dictionary<string, string> GetCurrentMessageHeaders()
        {
            return MessageContext.Current.Message.Headers;
        }
    }
}
