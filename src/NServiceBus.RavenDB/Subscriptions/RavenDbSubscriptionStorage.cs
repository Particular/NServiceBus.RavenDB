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
            var doNotCacheSubscriptions = context.Settings.GetOrDefault<bool>(DoNotCacheSubscriptions);
            var gotCacheSubscriptionsFor = context.Settings.TryGet(CacheSubscriptionsFor, out TimeSpan aggressiveCacheDuration);

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

        internal const string DoNotCacheSubscriptions = "RavenDB.DoNotAggressivelyCacheSubscriptions";
        internal const string CacheSubscriptionsFor = "RavenDB.AggressiveCacheDuration";
        static readonly ILog Log = LogManager.GetLogger<RavenDbSubscriptionStorage>();
    }
}