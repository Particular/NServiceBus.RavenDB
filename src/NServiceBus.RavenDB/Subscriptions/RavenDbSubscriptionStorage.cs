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
            var useClusterWideTransactions = context.Settings.GetOrDefault<bool>(RavenDbStorageSession.UseClusterWideTransactions);

            context.Settings.AddStartupDiagnosticsSection(
                "NServiceBus.Persistence.RavenDB.Subscriptions",
                new
                {
                    DoNotCacheSubscriptions = doNotCacheSubscriptions,
                    CacheSubscriptionsFor = cacheSubscriptionsFor,
                    ClusterWideTransactions = useClusterWideTransactions ? "Enabled" : "Disabled"
                });

            context.Container.ConfigureComponent<ISubscriptionStorage>(builder =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Subscriptions>(context.Settings, builder);

                return new SubscriptionPersister(store, useClusterWideTransactions)
                {
                    DisableAggressiveCaching = doNotCacheSubscriptions,
                    AggressiveCacheDuration = cacheSubscriptionsFor,
                };
            }, DependencyLifecycle.SingleInstance);
        }

        internal const string DoNotCacheSubscriptions = "RavenDB.DoNotAggressivelyCacheSubscriptions";
        internal const string CacheSubscriptionsFor = "RavenDB.AggressiveCacheDuration";
    }
}