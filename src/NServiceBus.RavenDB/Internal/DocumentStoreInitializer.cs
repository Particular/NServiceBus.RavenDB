namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using Raven.Client.Documents;
    using Raven.Client.ServerWide.Commands;

    class DocumentStoreInitializer
    {
        internal DocumentStoreInitializer(Func<ReadOnlySettings, IBuilder, IDocumentStore> storeCreator)
        {
            this.storeCreator = storeCreator;
        }

        internal DocumentStoreInitializer(IDocumentStore store)
        {
            storeCreator = (s,c) => store;
        }

        public string Identifier => docStore?.Identifier;

        internal IDocumentStore Init(ReadOnlySettings settings, IBuilder builder)
        {
            if (!isInitialized)
            {
                EnsureDocStoreCreated(settings, builder);
                ApplyConventions(settings);

                docStore.Initialize();
                EnsureClusterConfiguration(docStore);
            }
            isInitialized = true;
            return docStore;
        }

        void EnsureDocStoreCreated(ReadOnlySettings settings, IBuilder builder)
        {
            if (docStore == null)
            {
                docStore = storeCreator(settings, builder);
            }
        }

        void ApplyConventions(ReadOnlySettings settings)
        {
            var store = docStore as DocumentStore;
            if (store == null)
            {
                return;
            }

            UnwrappedSagaListener.Register(store);

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

        Func<ReadOnlySettings, IBuilder, IDocumentStore> storeCreator;
        IDocumentStore docStore;
        bool isInitialized;
    }
}
