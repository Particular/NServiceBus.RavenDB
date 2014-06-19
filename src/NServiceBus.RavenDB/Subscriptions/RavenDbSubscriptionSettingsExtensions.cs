namespace NServiceBus
{
    using Persistence;
    using Raven.Client;
    using RavenDB.Persistence;

    /// <summary>
    /// Provides configuration options specific to the subscription storage
    /// </summary>
    public static class RavenDbSubscriptionSettingsExtensions
    {
        internal const string SettingsKey = "RavenDbDocumentStore/Subscription";

        /// <summary>
        /// Configures the given document store to be used when storing subscriptions
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">The document store to use</param>
        /// <returns></returns>
        public static PersistenceConfiguration UseDocumentStoreForSubscriptions(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(SettingsKey, documentStore);
            RavenUserInstaller.AddDocumentStore(documentStore);
            return cfg;
        }
    }
}