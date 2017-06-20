﻿namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using Features;
    using Persistence;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
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

            var disableSubscriptionsVersioning = context.Settings.GetOrDefault<bool>(RavenDbSubscriptionSettingsExtensions.DisableSubscriptionsVersioningKey);
            var idFormatter = new SubscriptionIdFormatter(useMessageVersionToGenerateSubscriptionId: !disableSubscriptionsVersioning);
            var persister = new SubscriptionPersister(store, idFormatter);

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