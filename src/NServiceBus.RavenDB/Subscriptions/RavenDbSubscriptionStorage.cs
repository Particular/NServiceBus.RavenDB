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
            var doNotCacheSubscriptions = context.Settings.GetOrDefault<bool>(RavenDbSubscriptionSettingsExtensions.DoNotAggressivelyCacheSubscriptionsSettingsKey);
            var gotCacheSubscriptionsFor = context.Settings.TryGet(RavenDbSubscriptionSettingsExtensions.AggressiveCacheDurationSettingsKey, out TimeSpan aggressiveCacheDuration);

            context.Settings.AddStartupDiagnosticsSection(
                "NServiceBus.Persistence.RavenDB.Subscriptions",
                new
                {
                    DoNotCacheSubscriptions = doNotCacheSubscriptions,
                    CacheSubscriptionsFor = aggressiveCacheDuration,
                });

            context.Container.ConfigureComponent<ISubscriptionStorage>(b =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Subscriptions>(context.Settings, b);

                var persister = new SubscriptionPersister(store);

                if (doNotCacheSubscriptions)
                {
                    persister.DisableAggressiveCaching = true;
                }

                if (gotCacheSubscriptionsFor)
                {
                    persister.AggressiveCacheDuration = aggressiveCacheDuration;
                }

                return persister;
            }, DependencyLifecycle.SingleInstance);
        }

        static readonly ILog Log = LogManager.GetLogger<RavenDbSubscriptionStorage>();
    }
}