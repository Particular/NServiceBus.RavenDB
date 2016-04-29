namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;
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
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForSagas(this PersistenceExtentions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        ///     Configures the given document store to be used when storing sagas
        /// </summary>
        /// <param name="cfg">Object to attach to</param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForSagas(this PersistenceExtentions<RavenDBPersistence> cfg, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(cfg.GetSettings(), storeCreator);
            return cfg;
        }

        /// <summary>
        ///     Tells the saga persister that it should allow potential stale queries when loading sagas
        /// </summary>
        /// <param name="cfg">Object to attach to</param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "5", TreatAsErrorFromVersion = "4", Message = "As of Version 6 of NServiceBus core all correlated properties are unique by default so you can safely remove this setting.")]
        public static PersistenceExtentions<RavenDBPersistence> AllowStaleSagaReads(this PersistenceExtentions<RavenDBPersistence> cfg)
        {
            cfg.GetSettings().Set(AllowStaleSagaReadsKey, true);
            return cfg;
        }
    }
}