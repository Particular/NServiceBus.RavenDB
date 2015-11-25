namespace NServiceBus.Features
{
    using NServiceBus.SagaPersisters.RavenDB;

    /// <summary>
    ///     RavenDB Saga Storage.
    /// </summary>
    public class RavenDbSagaStorage : Feature
    {
        /// <summary>
        ///     Creates an instance of <see cref="RavenDbSagaStorage" />.
        /// </summary>
        internal RavenDbSagaStorage()
        {
            DependsOn<Sagas>();
        }

        /// <summary>
        ///     Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}