namespace NServiceBus.Features
{
    using System;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;
    using RavenDB;
    using RavenDB.Gateway.Deduplication;
    using RavenDB.Internal;

    class RavenDbGatewayDeduplication: Feature
    {
        RavenDbGatewayDeduplication()
        {
            DependsOn("Gateway");
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store =
                // Try getting a document store object specific to this Feature that user may have wired in
                context.Settings.GetOrDefault<IDocumentStore>(RavenDbGatewayDeduplicationSettingsExtensions.SettingsKey)
                // Init up a new DocumentStore based on a connection string specific to this feature
                ?? Helpers.CreateDocumentStoreByConnectionStringName(context.Settings, "NServiceBus/Persistence/RavenDB/GatewayDeduplication")
                // Trying pulling a shared DocumentStore set by the user or other Feature
                ?? context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence for GatewayDeduplication and no DocumentStore instance found");
            }

            // This is required for DTC fix, and this requires RavenDB 2.5 build 2900 or above
            var remoteStorage = store as DocumentStore;
            if (remoteStorage != null)
                remoteStorage.TransactionRecoveryStorage = new IsolatedStorageTransactionRecoveryStorage();

            ConnectionVerifier.VerifyConnectionToRavenDBServer(store);

            context.Container.ConfigureComponent<RavenDeduplication>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.DocumentStore, store);
        }
    }
}
