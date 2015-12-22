namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using NServiceBus.RavenDB;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.RavenDB;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;
    using Raven.Client.Linq;

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

            var doNotUpgradeSubscriptionSchema = 
                context.Settings.GetOrDefault<bool>(RavenDbSubscriptionSettingsExtensions.DoNotUpgradeSubscriptionSchema);

            ISubscriptionAccess subscriptionAccessMethod;

            if (doNotUpgradeSubscriptionSchema)
            {
                subscriptionAccessMethod = new AggregateSubscriptionDocumentAccess();
            }
            else
            {
                ConvertToIndividualDocumentSchema(store);
                subscriptionAccessMethod = new IndividualSubscriptionDocumentAccess();
            }

            context.Container.ConfigureComponent(() => new SubscriptionPersister(store, subscriptionAccessMethod), DependencyLifecycle.InstancePerCall);
        }

        private static void ConvertToIndividualDocumentSchema(IDocumentStore store)
        {
            using (var session = store.OpenSession())
            {
                var legacyDocuments = session.Load<Subscription>();
                var newDocuments = session.Load<SubscriptionDocument>();

                foreach (var legacyDocument in legacyDocuments)
                {
                    foreach (var subscriber in legacyDocument.Subscribers)
                    {
                        if (!newDocuments.Any(d => DocumentMatches(d, legacyDocument.MessageType, subscriber)))
                        {
                            session.Store(new SubscriptionDocument()
                            {
                                MessageType = legacyDocument.MessageType,
                                SubscriptionClient = subscriber
                            });
                        }
                    }
                }

                session.SaveChanges();
            }
        }

        private static bool DocumentMatches(SubscriptionDocument newDoc, MessageType messageType, SubscriptionClient client)
        {
            return messageType.Equals(newDoc.MessageType) && client.Equals(newDoc.SubscriptionClient);
        }
    }
}