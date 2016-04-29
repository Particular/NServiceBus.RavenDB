namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.Features;
    using NServiceBus.Persistence;

    class RavenDbSubscriptionStorage : Feature
    {
        RavenDbSubscriptionStorage()
        {
            DependsOn<SharedDocumentStore>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store = DocumentStoreManager.GetDocumentStore<StorageType.Subscriptions>(context.Settings);

            store.Listeners.RegisterListener(new SubscriptionV1toV2Converter());

            context.Container.ConfigureComponent(() => new SubscriptionPersister(store), DependencyLifecycle.InstancePerCall);
        }
    }
}