namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using Raven.Client;

    /// <summary>
    ///     Provides configuration options specific to the subscription storage
    /// </summary>
    public static class RavenDbSubscriptionSettingsExtensions
    {
        internal const string SettingsKey = "RavenDbDocumentStore/Subscription";

        /// <summary>
        ///     Configures the given document store to be used when storing subscriptions
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">The document store to use</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForSubscriptions(this PersistenceExtentions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            cfg.GetSettings().Set(SettingsKey, documentStore);
            return cfg;
        }
    }
}