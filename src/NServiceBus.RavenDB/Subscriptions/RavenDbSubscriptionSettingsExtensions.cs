namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;
    using Raven.Client.Documents;

    /// <summary>
    /// Provides configuration options specific to the subscription storage
    /// </summary>
    public static class RavenDbSubscriptionSettingsExtensions
    {
        /// <summary>
        /// Configures the given document store to be used when storing subscriptions
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="documentStore">The document store to use</param>
        public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForSubscriptions(this PersistenceExtensions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Subscriptions>(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        /// Configures the given document store to be used when storing subscriptions
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForSubscriptions(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IReadOnlySettings, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Subscriptions>(cfg.GetSettings(), storeCreator);
            return cfg;
        }

        /// <summary>
        /// Configures the given document store to be used when storing subscriptions
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForSubscriptions(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IReadOnlySettings, IServiceProvider, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Subscriptions>(cfg.GetSettings(), storeCreator);
            return cfg;
        }

        /// <summary>
        /// Disable in-memory caching of Subscription information. By default, NServiceBus will cache subscriptions in memory
        /// until a server notification informs the RavenDB client of a change, or 1 minute elapses, whichever occurs first.
        /// Although slower, using this option ensures that the subscription storage is checked for changes with every published message.
        /// </summary>
        public static PersistenceExtensions<RavenDBPersistence> DoNotCacheSubscriptions(this PersistenceExtensions<RavenDBPersistence> cfg)
        {
            cfg.GetSettings().Set(RavenDbSubscriptionStorage.DoNotCacheSubscriptions, true);
            return cfg;
        }

        /// <summary>
        /// Change the amount of time that Subscription information is cached in-memory. Uses the RavenDB Aggressive Caching feature,
        /// so RavenDB server will send notifications to the client when subscriptions change before the cache duration expires,
        /// however these notifications are not 100% reliable. Default duration is 1 minute.
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="aggressiveCacheDuration">The time to cache subscription information</param>
        public static PersistenceExtensions<RavenDBPersistence> CacheSubscriptionsFor(this PersistenceExtensions<RavenDBPersistence> cfg, TimeSpan aggressiveCacheDuration)
        {
            cfg.GetSettings().Set(RavenDbSubscriptionStorage.CacheSubscriptionsFor, aggressiveCacheDuration);
            return cfg;
        }
    }
}