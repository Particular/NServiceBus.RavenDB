namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    class RavenDbSubscriptionStorage : Feature
    {
        RavenDbSubscriptionStorage()
        {
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var doNotCacheSubscriptions = context.Settings.GetOrDefault<bool>(DoNotCacheSubscriptions);
            var cacheSubscriptionsFor = context.Settings.GetOrDefault<TimeSpan?>(CacheSubscriptionsFor) ?? TimeSpan.FromMinutes(1);

            context.Settings.AddStartupDiagnosticsSection(
                "NServiceBus.Persistence.RavenDB.Subscriptions",
                new
                {
                    DoNotCacheSubscriptions = doNotCacheSubscriptions,
                    CacheSubscriptionsFor = cacheSubscriptionsFor,
                });

            context.Container.ConfigureComponent<ISubscriptionStorage>(builder =>
                {
                    var store = DocumentStoreManager.GetDocumentStore<StorageType.Subscriptions>(context.Settings, builder);

                    var persister = new SubscriptionPersister(store);

                    if (doNotCacheSubscriptions)
                    {
                        persister.DisableAggressiveCaching = true;
                    }

                    persister.AggressiveCacheDuration = cacheSubscriptionsFor;

                    return persister;
                },
                DependencyLifecycle.SingleInstance);
        }

        internal const string DoNotCacheSubscriptions = "RavenDB.DoNotAggressivelyCacheSubscriptions";
        internal const string CacheSubscriptionsFor = "RavenDB.AggressiveCacheDuration";
    }
}
