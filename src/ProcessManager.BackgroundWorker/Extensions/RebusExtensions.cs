using Autofac;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Retry.FailFast;
using Rebus.Topic;

namespace ProcessManager.BackgroundWorker.Extensions
{
    public static class RebusExtensions
    {
        public static void UseContainerScopeInitializerStep(this OptionsConfigurer configurer, ILifetimeScope lifetimeScope)
        {
            configurer.Decorate<IPipeline>(c =>
            {
                var incomingStep = new ContainerScopeInitializerStep(lifetimeScope);

                var pipeline = c.Get<IPipeline>();

                return new PipelineStepInjector(pipeline)
                    .OnReceive(incomingStep, PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep));
            });
        }

        public static void EnableMessageDeDuplication(this OptionsConfigurer configurer)
        {
            configurer.Decorate<IPipeline>(c =>
            {
                var incomingStep = new MessageDeDuplicationIncomingStep();

                var pipeline = c.Get<IPipeline>();

                return new PipelineStepInjector(pipeline)
                    .OnReceive(incomingStep, PipelineRelativePosition.After, typeof(ContainerScopeInitializerStep));
            });
        }

        public static void UseFailFastChecker(this OptionsConfigurer configurer)
        {
            configurer.Decorate<IFailFastChecker>(c => {
                var failFastChecker = c.Get<IFailFastChecker>();
                return new FailFastCheckerStep(failFastChecker);
            });
        }
    }
}
