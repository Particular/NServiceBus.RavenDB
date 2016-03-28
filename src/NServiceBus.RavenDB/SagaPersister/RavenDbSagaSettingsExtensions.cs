﻿namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using NServiceBus.RavenDB.Internal;
    using Raven.Client;

    /// <summary>
    ///     Provides configuration options
    /// </summary>
    public static class RavenDbSagaSettingsExtensions
    {
        internal const string AllowStaleSagaReadsKey = "RavenDB.AllowStaleSagaReads";

        /// <summary>
        ///     Configures the given document store to be used when storing sagas
        /// </summary>
        /// <param name="cfg">Object to attach to</param>
        /// <param name="documentStore">The document store to be used</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForSagas(this PersistenceExtentions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        ///     Tells the saga persister that it should allow potential stale queries when loading sagas
        /// </summary>
        /// <param name="cfg">Object to attach to</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> AllowStaleSagaReads(this PersistenceExtentions<RavenDBPersistence> cfg)
        {
            cfg.GetSettings().Set(AllowStaleSagaReadsKey, true);
            return cfg;
        }
    }
}