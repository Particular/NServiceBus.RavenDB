namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.Features;

    class RavenDbSagaStorage : Feature
    {
        internal RavenDbSagaStorage()
        {
            DependsOn<Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}