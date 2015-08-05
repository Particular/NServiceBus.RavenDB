namespace NServiceBus.Features
{
    using System;
    using NServiceBus.RavenDB;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.RavenDB.Timeouts;
    using NServiceBus.Unicast.Subscriptions.RavenDB;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    class RavenDbSubscriptionStorage : Feature
    {
        RavenDbSubscriptionStorage()
        {
            DependsOn<SharedDocumentStore>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store =
                // Try getting a document store object specific to this Feature that user may have wired in
                context.Settings.GetOrDefault<IDocumentStore>(RavenDbSubscriptionSettingsExtensions.SettingsKey)
                    // Init up a new DocumentStore based on a connection string specific to this feature
                ?? Helpers.CreateDocumentStoreByConnectionStringName(context.Settings, "NServiceBus/Persistence/RavenDB/Subscription")
                    // Trying pulling a shared DocumentStore set by the user or other Feature
                ?? context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Subscriptions and no DocumentStore instance found");
            }

            ConnectionVerifier.VerifyConnectionToRavenDBServer(store);
            StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(store, context.Settings);

            BackwardsCompatibilityHelper.SupportOlderClrTypes(store);

            // This is required for DTC fix, and this requires RavenDB 2.5 build 2900 or above
            var remoteStorage = store as DocumentStore;
            if (remoteStorage != null)
            {
                remoteStorage.TransactionRecoveryStorage = new IsolatedStorageTransactionRecoveryStorage();
            }

            store.Listeners.RegisterListener(new SubscriptionV1toV2Converter());

            context.Container.ConfigureComponent(() => new SubscriptionPersister(store), DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent(() => new QuerySubscriptions(store), DependencyLifecycle.InstancePerCall);
        }
    }
}