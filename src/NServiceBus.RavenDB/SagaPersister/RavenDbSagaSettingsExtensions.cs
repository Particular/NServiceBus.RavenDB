namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using Raven.Client;

    /// <summary>
    ///     Provides configuration options
    /// </summary>
    public static class RavenDbSagaSettingsExtensions
    {
        internal const string DocumentStoreSettingsKey = "RavenDbDocumentStore/Saga";
        internal const string AllowStaleSagaReadsKey = "RavenDB.AllowStaleSagaReads";

        /// <summary>
        ///     Configures the given document store to be used when storing sagas
        /// </summary>
        /// <param name="cfg">Object to attach to</param>
        /// <param name="documentStore">The document store to be used</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForSagas(this PersistenceExtentions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            cfg.GetSettings().Set(DocumentStoreSettingsKey, documentStore);
            return cfg;
        }

        /// <summary>
        ///     Tells the saga persister that it should allow potential stale queries when loading sagas
        /// </summary>
        /// <param name="cfg">Object to attach to</param>
        /// <returns></returns>
       [ObsoleteEx(RemoveInVersion = "5", TreatAsErrorFromVersion = "4",Message = "As of Version 6 of NServiceBus core all correlated properties are unique by default so you can safely remove this setting.")]
        public static PersistenceExtentions<RavenDBPersistence> AllowStaleSagaReads(this PersistenceExtentions<RavenDBPersistence> cfg)
        {
            cfg.GetSettings().Set(AllowStaleSagaReadsKey, true);
            return cfg;
        }
    }
}