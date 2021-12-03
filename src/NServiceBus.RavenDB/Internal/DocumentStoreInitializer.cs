namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Indexes;
    using Raven.Client.Documents.Operations.Indexes;
    using Raven.Client.ServerWide.Commands;
    using Raven.Client.ServerWide.Operations;

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
                    {
                        throw;
                    }
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
                EnsureCompatibleServerVersion(docStore);
                var useClusterWideTx = settings.GetOrDefault<bool>(RavenDbStorageSession.UseClusterWideTransactions);
                EnsureClusterConfiguration(docStore, useClusterWideTx);

                CreateIndexes(docStore);
            }

            isInitialized = true;
            return docStore;
        }

        void EnsureCompatibleServerVersion(IDocumentStore documentStore)
        {
            var requiredVersion = new Version(5, 2);
            var serverVersion = documentStore.Maintenance.Server.Send(new GetBuildNumberOperation());
            var fullVersion = new Version(serverVersion.FullVersion);

            if (fullVersion.Major < requiredVersion.Major ||
                (fullVersion.Major == requiredVersion.Major && fullVersion.Minor < requiredVersion.Minor))
            {
                throw new Exception($"We detected that the server is running on version {serverVersion.FullVersion}. RavenDB persistence requires RavenDB server 5.2 or higher");
            }
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
            if (isSendOnly)
            {
                return;
            }

            var usingDtc = settings.GetRequiredTransactionModeForReceives() == TransportTransactionMode.TransactionScope;
            if (usingDtc)
            {
                throw new Exception("RavenDB does not support Distributed Transaction Coordinator (DTC) transactions. You must change the TransportTransactionMode in order to continue. See the RavenDB Persistence documentation for more details.");
            }
        }

        static void EnsureClusterConfiguration(IDocumentStore store, bool useClusterWideTransactions)
        {
            using (var s = store.OpenSession())
            {
                var databaseTopology = new GetDatabaseTopologyCommand();
                s.Advanced.RequestExecutor.Execute(databaseTopology, s.Advanced.Context);
                var clusterTopology = new GetClusterTopologyCommand();
                s.Advanced.RequestExecutor.Execute(clusterTopology, s.Advanced.Context);

                if (useClusterWideTransactions && databaseTopology.Result.Nodes.Count == 1 && clusterTopology.Result.Topology.AllNodes.Count > 1)
                {
                    Logger.Warn($"The replication factor of the configured database is 1, while more nodes were detected in the cluster. If the intent is to keep the replication factor as configured, do not enable {nameof(RavenDbSettingsExtensions.EnableClusterWideTransactions)} on the persistence configuration since it comes with performance implications.");
                }
                else if (!useClusterWideTransactions && databaseTopology.Result.Nodes.Count > 1)
                {
                    throw new Exception($"The configured database is replicated across multiple nodes, in order to continue, use {nameof(RavenDbSettingsExtensions.EnableClusterWideTransactions)} on the persistence configuration.");
                }
            }
        }

        List<AbstractIndexCreationTask> indexesToCreate = new List<AbstractIndexCreationTask>();
        Func<ReadOnlySettings, IBuilder, IDocumentStore> storeCreator;
        IDocumentStore docStore;
        bool isInitialized;
        static readonly ILog Logger = LogManager.GetLogger(typeof(DocumentStoreInitializer));
    }
}
