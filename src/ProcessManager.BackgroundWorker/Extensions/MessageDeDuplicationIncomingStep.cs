using System;
using System.Threading.Tasks;
using Autofac;
using ProcessManager.Domain.Exceptions;
using ProcessManager.Domain.Interfaces;
using Rebus.Messages;
using Rebus.Pipeline;

namespace ProcessManager.BackgroundWorker.Extensions
{
    public class MessageDeDuplicationIncomingStep : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var messageId = GetMessageId(message);
            var scope = context.Load<ILifetimeScope>();
            var outboxRepository = scope.Resolve<IOutboxRepository>();

            if (messageId != Guid.Empty && await outboxRepository.CheckIfExists(messageId))
            {
                throw new DuplicatedMessageException(message.Body.GetType().Name, messageId.ToString());
            }

            await next();
        }

        private Guid GetMessageId(Message message)
        {
            var headers = message.Headers;
            headers.TryGetValue("x-command-id", out var messageIdHeaderValue);

            if (!string.IsNullOrEmpty(messageIdHeaderValue))
            {
                Guid.TryParse(messageIdHeaderValue, out var messageId);
                return messageId;
            }
            return Guid.Empty;
        }
    }
}
