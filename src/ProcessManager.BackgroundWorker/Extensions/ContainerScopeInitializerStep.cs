using System;
using System.Threading.Tasks;
using Autofac;
using Rebus.Pipeline;
using Rebus.Transport;

namespace ProcessManager.BackgroundWorker.Extensions
{
    public class ContainerScopeInitializerStep : IIncomingStep
    {
        private readonly ILifetimeScope _lifetimeScope;

        public ContainerScopeInitializerStep(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transactionContext = context.Load<ITransactionContext>();

            ILifetimeScope CreateLifetimeScope()
            {
                var scope = _lifetimeScope.BeginLifetimeScope();
                transactionContext.OnDisposed(ctx => scope.Dispose());
                return scope;
            }

            var lifetimeScope = transactionContext.GetOrAdd("current-autofac-lifetime-scope", CreateLifetimeScope);
            context.Save(lifetimeScope);
            await next();
        }
    }
}
