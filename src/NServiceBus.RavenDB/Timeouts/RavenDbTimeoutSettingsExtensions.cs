namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using Persistence;
    using Raven.Client;

    /// <summary>
    /// Configuration settings specific to the timeout storage
    /// </summary>
    public static class RavenDbTimeoutSettingsExtensions
    {
        internal const string SettingsKey = "RavenDbDocumentStore/Timeouts";

        /// <summary>
        /// Configures the given document store to be used when storing timeouts
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">The document store to use</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForTimeouts(this PersistenceExtentions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            cfg.GetSettings().Set(SettingsKey, documentStore);
            return cfg;
        }
    }
}