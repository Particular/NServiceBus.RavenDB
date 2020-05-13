namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Indexes;
    using Raven.Client.Documents.Operations.Indexes;
    using Raven.Client.ServerWide.Commands;

    class DocumentStoreInitializer
    {
        internal DocumentStoreInitializer(Func<ReadOnlySettings, IBuilder, IDocumentStore> storeCreator)
        {
            this.storeCreator = storeCreator;
        }

        internal DocumentStoreInitializer(IDocumentStore store)
        {
            storeCreator = (s, c) => store;
        }

        public string Identifier => docStore?.Identifier;

        /// <summary>
        /// Safely add the index to the RavenDB database, protect against possible failures caused by documented
        /// and undocumented possibilities of failure.
        /// Will throw iff index registration failed and index doesn't exist or it exists but with a non-current definition.
        /// </summary>
        internal void CreateIndexOnInitialization(AbstractIndexCreationTask index)
        {
            indexesToCreate.Add(index);
        }

        void CreateIndexes(IDocumentStore store)
        {
            foreach (var index in indexesToCreate)
            {
                try
                {
                    index.Execute(store);
                }
                catch (Exception) // Apparently ArgumentException can be thrown as well as a WebException; not taking any chances
                {
                    var getIndexOp = new GetIndexOperation(index.IndexName);

                    var existingIndex = store.Maintenance.Send(getIndexOp);
                    if (existingIndex == null || !index.CreateIndexDefinition().Equals(existingIndex))
                        throw;
                }
            }
        }

        internal IDocumentStore Init(ReadOnlySettings settings, IBuilder builder)
        {
            if (!isInitialized)
            {
                EnsureDocStoreCreated(settings, builder);
                ApplyConventions(settings);

                docStore.Initialize();
                EnsureClusterConfiguration(docStore);

                CreateIndexes(docStore);
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
            if (!(docStore is DocumentStore store))
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

        static void EnsureClusterConfiguration(IDocumentStore store)
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

        List<AbstractIndexCreationTask> indexesToCreate = new List<AbstractIndexCreationTask>();
        Func<ReadOnlySettings, IBuilder, IDocumentStore> storeCreator;
        IDocumentStore docStore;
        bool isInitialized;
    }
}