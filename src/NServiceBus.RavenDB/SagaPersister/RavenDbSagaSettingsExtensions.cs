namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using Persistence;
    using Raven.Client;

    /// <summary>
    /// Provides configuration options
    /// </summary>
    public static class RavenDbSagaSettingsExtensions
    {
        internal const string SettingsKey = "RavenDbDocumentStore/Saga";

        /// <summary>
        /// Configures the given document store to be used when storing sagas
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">The document store to be used</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForSagas(this PersistenceExtentions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            cfg.GetSettings().Set(SettingsKey, documentStore);
            return cfg;
        }
    }
}