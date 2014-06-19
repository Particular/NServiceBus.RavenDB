namespace NServiceBus
{
    using Persistence;
    using Raven.Client;
    using RavenDB.Persistence;

    /// <summary>
    /// Configuration settings specific to the timeout storage
    /// </summary>
    public static class RavenDbGatewayDeduplicationSettingsExtensions
    {
        internal const string SettingsKey = "RavenDbDocumentStore/GatewayDeduplication";

        /// <summary>
        /// Configures the given document store to be used when storing gateway deduplication data
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">The document store to use</param>
        /// <returns></returns>
        public static PersistenceConfiguration UseDocumentStoreForGatewayDeduplication(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(SettingsKey, documentStore);
            RavenUserInstaller.AddDocumentStore(documentStore);
            return cfg;
        }
    }
}