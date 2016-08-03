namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;
    using Raven.Client;

    /// <summary>
    ///     Provides configuration options specific to the subscription storage
    /// </summary>
    public static class RavenDbSubscriptionSettingsExtensions
    {
        internal const string DoNotAggressivelyCacheSubscriptionsSettingsKey = "RavenDB.DoNotAggressivelyCacheSubscriptions";
        internal const string AggressiveCacheDurationSettingsKey = "RavenDB.DoNotAggressivelyCacheSubscriptions";

        /// <summary>
        ///     Configures the given document store to be used when storing subscriptions
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">The document store to use</param>
        public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForSubscriptions(this PersistenceExtensions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Subscriptions>(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        ///     Configures the given document store to be used when storing subscriptions
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForSubscriptions(this PersistenceExtensions<RavenDBPersistence> cfg, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Subscriptions>(cfg.GetSettings(), storeCreator);
            return cfg;
        }

        /// <summary>
        /// Disable in-memory caching of Subscription information. By default, NServiceBus will cache this in memory
        /// until a server notification informs the RavenDB client of a change, or 1 minute elapses, whichever occurs first.
        /// Although slower, using this option ensures that the subscription storage is checked for changes with every published message.
        /// </summary>
        /// <param name="cfg"></param>
        public static PersistenceExtensions<RavenDBPersistence> DoNotAggressivelyCacheSubscriptions(this PersistenceExtensions<RavenDBPersistence> cfg)
        {
            cfg.GetSettings().Set(DoNotAggressivelyCacheSubscriptionsSettingsKey, true);
            return cfg;
        }

        /// <summary>
        /// Change the amount of time that Subscription information is cached in-memory. Use of RavenDB server notifications generally
        /// result in cached information being invalidated much earlier when a change is detected on the server, but these 
        /// notifications are not 100% reliable.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="aggressiveCacheDuration"></param>
        /// <returns></returns>
        public static PersistenceExtensions<RavenDBPersistence> AggressivelyCacheSubscriptionsFor(this PersistenceExtensions<RavenDBPersistence> cfg, TimeSpan aggressiveCacheDuration)
        {
            cfg.GetSettings().Set(AggressiveCacheDurationSettingsKey, aggressiveCacheDuration);
            return cfg;
        }
    }
}