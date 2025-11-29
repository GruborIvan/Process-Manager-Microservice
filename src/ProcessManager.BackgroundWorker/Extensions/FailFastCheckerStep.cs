using System;
using ProcessManager.Domain.Exceptions;
using Rebus.Retry.FailFast;

namespace ProcessManager.BackgroundWorker.Extensions
{
    public class FailFastCheckerStep : IFailFastChecker
    {
        readonly IFailFastChecker _failFastChecker;

        public FailFastCheckerStep(IFailFastChecker failFastChecker)
        {
            _failFastChecker = failFastChecker;
        }

        public bool ShouldFailFast(string messageId, Exception exception)
        {
            switch (exception)
            {
                // fail fast on our domain exception
                case DuplicatedMessageException _: return true;

                // delegate all other behavior to default
                default: return _failFastChecker.ShouldFailFast(messageId, exception);
            }
        }
    }
}
