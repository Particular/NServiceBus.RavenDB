namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Persistence;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;

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

            var persister = new SubscriptionPersister(store);

            if (context.Settings.GetOrDefault<bool>(RavenDbSubscriptionSettingsExtensions.DoNotAggressivelyCacheSubscriptionsSettingsKey))
            {
                persister.DisableAggressiveCaching = true;
            }

            if (context.Settings.TryGet(RavenDbSubscriptionSettingsExtensions.LegacySubscriptionVersioningKey, out bool useLegacy))
            {
                if (useLegacy)
                {
                    // This is the default in the persister class, to facilitate tests verifying legacy behavior
                    Log.Warn("RavenDB Persistence is using legacy versioned subscription storage. This capability will be removed in NServiceBus.RavenDB 6.0.0. Subscription documents need to be converted to the new unversioned format, after which `persistence.DisableSubscriptionVersioning()` should be used.");
                }
                else
                {
                    persister.SubscriptionIdFormatter = new VersionedSubscriptionIdFormatter();
                }
            }
            else
            {
                throw new Exception("RavenDB subscription storage requires using either `persistence.DisableSubscriptionVersioning()` or `persistence.UseLegacyVersionedSubscriptions()` to determine whether legacy versioned subscriptions should be used.");
            }

            if (context.Settings.TryGet(RavenDbSubscriptionSettingsExtensions.AggressiveCacheDurationSettingsKey, out TimeSpan aggressiveCacheDuration))
            {
                persister.AggressiveCacheDuration = aggressiveCacheDuration;
            }

            context.Container.ConfigureComponent<ISubscriptionStorage>(_ => persister, DependencyLifecycle.SingleInstance);
        }

        static readonly ILog Log = LogManager.GetLogger<RavenDbSubscriptionStorage>();
    }
}