namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Persistence;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

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

            SubscriptionIndex.Create(store);

            var persister = new SubscriptionPersister(store);

            if (context.Settings.GetOrDefault<bool>(RavenDbSubscriptionSettingsExtensions.DoNotAggressivelyCacheSubscriptionsSettingsKey))
            {
                persister.DisableAggressiveCaching = true;
            }

            TimeSpan aggressiveCacheDuration;
            if (context.Settings.TryGet(RavenDbSubscriptionSettingsExtensions.AggressiveCacheDurationSettingsKey, out aggressiveCacheDuration))
            {
                persister.AggressiveCacheDuration = aggressiveCacheDuration;
            }

            context.Container.ConfigureComponent<ISubscriptionStorage>(_ => persister, DependencyLifecycle.SingleInstance);
        }
    }
}