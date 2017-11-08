namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using Raven.Client;
    using Raven.Client.Document;

    class DocumentStoreInitializer
    {
        internal DocumentStoreInitializer(Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            this.storeCreator = storeCreator;
        }

        internal DocumentStoreInitializer(IDocumentStore store)
        {
            storeCreator = readOnlySettings => store;
        }

        public string Url => docStore?.Url;

        public string Identifier => docStore?.Identifier;

        internal IDocumentStore Init(ReadOnlySettings settings)
        {
            if (!isInitialized)
            {
                EnsureDocStoreCreated(settings);
                ApplyConventions(settings);
                BackwardsCompatibilityHelper.SupportOlderClrTypes(docStore);

                docStore.Initialize();
            }
            isInitialized = true;
            return docStore;
        }

        internal void EnsureDocStoreCreated(ReadOnlySettings settings)
        {
            if (docStore == null)
            {
                docStore = storeCreator(settings);
            }
        }

        void ApplyConventions(ReadOnlySettings settings)
        {
            if (DocumentIdConventionsExtensions.NeedToApplyDocumentIdConventionsToDocumentStore(settings))
            {
                var sagasEnabled = settings.IsFeatureActive(typeof(Sagas));
                var timeoutsEnabled = settings.IsFeatureActive(typeof(TimeoutManager));
                var idConventions = new DocumentIdConventions(docStore, settings.GetAvailableTypes(), settings.EndpointName(), sagasEnabled, timeoutsEnabled);
                docStore.Conventions.FindTypeTagName = idConventions.FindTypeTagName;
            }

            var store = docStore as DocumentStore;
            if (store == null)
            {
                return;
            }

            var isSendOnly = settings.GetOrDefault<bool>("Endpoint.SendOnly");
            if (!isSendOnly)
            {
                var usingDtc = settings.GetRequiredTransactionModeForReceives() == TransportTransactionMode.TransactionScope;
                if (usingDtc)
                {
                    throw new Exception("RavenDB Persistence does not support Distributed Transaction Coordinator (DTC) transactions. You must change the TransportTransactionMode in order to continue. See the RavenDB Persistence documentation for more details.");
                }
            }

            store.EnlistInDistributedTransactions = false;
        }

        Func<ReadOnlySettings, IDocumentStore> storeCreator;
        IDocumentStore docStore;
        bool isInitialized;
    }
}
