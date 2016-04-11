namespace NServiceBus.Features
{
    using NServiceBus.SagaPersisters.RavenDB;

    class RavenDbSagaStorage : Feature
    {
        internal RavenDbSagaStorage()
        {
            DependsOn<Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}