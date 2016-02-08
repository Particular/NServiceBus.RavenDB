namespace NServiceBus.Features
{
    using NServiceBus.Persistence;
    using NServiceBus.RavenDB;
    using NServiceBus.RavenDB.Internal;
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

            Helpers.SafelyCreateIndex(store, new TimeoutsIndex());

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(c => c.StoreToInstall, store);

            context.Container.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.DocumentStore, store)
                .ConfigureProperty(x => x.EndpointName, context.Settings.EndpointName());
        }
    }
}
