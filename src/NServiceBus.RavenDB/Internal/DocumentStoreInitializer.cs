namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Settings;
    using Raven.Client.Documents;
    using Raven.Client.ServerWide.Commands;

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

        public string Identifier => docStore?.Identifier;

        internal IDocumentStore Init(ReadOnlySettings settings)
        {
            if (!isInitialized)
            {
                EnsureDocStoreCreated(settings);
                ApplyConventions(settings);

                docStore.Initialize();
                EnsureClusterConfiguration(docStore);
            }
            isInitialized = true;
            return docStore;
        }

        void EnsureDocStoreCreated(ReadOnlySettings settings)
        {
            if (docStore == null)
            {
                docStore = storeCreator(settings);
            }
        }

        void ApplyConventions(ReadOnlySettings settings)
        {
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
                    throw new Exception("RavenDB does not support Distributed Transaction Coordinator (DTC) transactions. You must change the TransportTransactionMode in order to continue. See the RavenDB Persistence documentation for more details.");
                }
            }
        }

        void EnsureClusterConfiguration(IDocumentStore store)
        {
            using (var s = store.OpenSession())
            {
                var getTopologyCmd = new GetClusterTopologyCommand();
                s.Advanced.RequestExecutor.Execute(getTopologyCmd, s.Advanced.Context);

                var topology = getTopologyCmd.Result.Topology;

                // Currently do not support clusters with more than one possible primary member. Watchers (passive replication targets) are OK.
                if (topology.Members.Count != 1)
                {
                    throw new InvalidOperationException("RavenDB Persistence does not support RavenDB clusters with more than one Leader/Member node. Only clusters with a single Leader and (optionally) Watcher nodes are supported.");
                }
            }
        }

        Func<ReadOnlySettings, IDocumentStore> storeCreator;
        IDocumentStore docStore;
        bool isInitialized;
    }
}
