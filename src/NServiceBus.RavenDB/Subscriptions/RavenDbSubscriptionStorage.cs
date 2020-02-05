namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    class RavenDbSubscriptionStorage : Feature
    {
        RavenDbSubscriptionStorage()
        {
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<ISubscriptionStorage>(b =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Subscriptions>(context.Settings, b);

                var persister = new SubscriptionPersister(store);

                if (context.Settings.GetOrDefault<bool>(RavenDbSubscriptionSettingsExtensions.DoNotAggressivelyCacheSubscriptionsSettingsKey))
                {
                    persister.DisableAggressiveCaching = true;
                }

                if (context.Settings.TryGet(RavenDbSubscriptionSettingsExtensions.AggressiveCacheDurationSettingsKey, out TimeSpan aggressiveCacheDuration))
                {
                    persister.AggressiveCacheDuration = aggressiveCacheDuration;
                }

                return persister;
            }, DependencyLifecycle.SingleInstance);
        }

        static readonly ILog Log = LogManager.GetLogger<RavenDbSubscriptionStorage>();
    }
}