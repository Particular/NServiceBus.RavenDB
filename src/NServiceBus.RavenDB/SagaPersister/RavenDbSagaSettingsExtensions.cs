namespace NServiceBus
{
    using Persistence;
    using Raven.Client;
    using RavenDB.Persistence;

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
        public static PersistenceConfiguration UseDocumentStoreForSagas(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(SettingsKey, documentStore);
            RavenUserInstaller.AddDocumentStore(documentStore);
            return cfg;
        }
    }
}