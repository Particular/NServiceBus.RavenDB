namespace NServiceBus.Features
{
    using NServiceBus.Persistence;
    using NServiceBus.RavenDB;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.RavenDB.Timeouts;
    using NServiceBus.TimeoutPersisters.RavenDB;

    class RavenDbTimeoutStorage : Feature
    {
        RavenDbTimeoutStorage()
        {
            DependsOn<TimeoutManager>();
            DependsOn<SharedDocumentStore>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings);

            store.Listeners.RegisterListener(new TimeoutDataV1toV2Converter());

            Helpers.SafelyCreateIndex(store, new TimeoutsIndex());

            context.Container.ConfigureComponent(() => new TimeoutPersister(store), DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent(() => new QueryTimeouts(store, context.Settings.EndpointName().ToString()), DependencyLifecycle.SingleInstance); // Needs to be SingleInstance because it contains cleanup state
        }
    }
}
