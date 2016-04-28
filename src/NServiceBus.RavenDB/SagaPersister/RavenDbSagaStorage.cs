namespace NServiceBus.Features
{
    using NServiceBus.SagaPersisters.RavenDB;

    /// <summary>
    ///     RavenDB Saga Storage.
    /// </summary>
    [ObsoleteEx(Message = "This type was not meant to be used in external code is being made internal.", RemoveInVersion = "5", TreatAsErrorFromVersion = "4")]
    public class RavenDbSagaStorage : Feature
    {
        internal RavenDbSagaStorage()
        {
            DependsOn<Sagas>();
        }

        /// <summary>
        ///     Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}