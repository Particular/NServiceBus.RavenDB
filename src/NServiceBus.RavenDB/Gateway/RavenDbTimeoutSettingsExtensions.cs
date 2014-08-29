namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using Raven.Client;

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
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForGatewayDeduplication(this PersistenceExtentions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            cfg.GetSettings().Set(SettingsKey, documentStore);
            return cfg;
        }
    }
}