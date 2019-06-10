namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;
    using Raven.Client.Documents;

    /// <summary>
    ///     Provides configuration options specific to the subscription storage
    /// </summary>
    public static class RavenDbSubscriptionSettingsExtensions
    {
        internal const string DoNotAggressivelyCacheSubscriptionsSettingsKey = "RavenDB.DoNotAggressivelyCacheSubscriptions";
        internal const string AggressiveCacheDurationSettingsKey = "RavenDB.AggressiveCacheDuration";

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
        /// Disable in-memory caching of Subscription information. By default, NServiceBus will cache subscriptions in memory
        /// until a server notification informs the RavenDB client of a change, or 1 minute elapses, whichever occurs first.
        /// Although slower, using this option ensures that the subscription storage is checked for changes with every published message.
        /// </summary>
        /// <param name="cfg"></param>
        public static PersistenceExtensions<RavenDBPersistence> DoNotCacheSubscriptions(this PersistenceExtensions<RavenDBPersistence> cfg)
        {
            cfg.GetSettings().Set(DoNotAggressivelyCacheSubscriptionsSettingsKey, true);
            return cfg;
        }

        /// <summary>
        /// Change the amount of time that Subscription information is cached in-memory. Uses the RavenDB Aggresive Caching feature,
        /// so RavenDB server will send notifications to the client when subscriptions change before the cache duration expires,
        /// however these notifications are not 100% reliable. Default duration is 1 minute.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="aggressiveCacheDuration"></param>
        /// <returns></returns>
        public static PersistenceExtensions<RavenDBPersistence> CacheSubscriptionsFor(this PersistenceExtensions<RavenDBPersistence> cfg, TimeSpan aggressiveCacheDuration)
        {
            cfg.GetSettings().Set(AggressiveCacheDurationSettingsKey, aggressiveCacheDuration);
            return cfg;
        }

        /// <summary>
        /// Do not include message assembly major version in subscription document lookup key.
        /// Subscription behavior will be changed such that all existing subscriptions will be rendered invalid.
        /// Do not enable in an existing system without converting subscription documents first.
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        [ObsoleteEx(
            Message = "Subscriptions must be converted to the new format.",
            TreatAsErrorFromVersion = "6.0.0",
            RemoveInVersion = "7.0.0")]
        public static PersistenceExtensions<RavenDBPersistence> DisableSubscriptionVersioning(this PersistenceExtensions<RavenDBPersistence> cfg)
        {
            throw new NotImplementedException("Subscriptions must be converted to the new format.");
        }

        /// <summary>
        /// Continue to use legacy method of subscription versioning that includes the message assembly
        /// major version in the subscription document lookup key. This option will be obsoleted in NServiceBus.RavenDB 6.0.0,
        /// so subscriptions should be converted to the new format, after which the <see cref="DisableSubscriptionVersioning"/>
        /// option should be used instead.
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        [ObsoleteEx(
            Message = "Subscriptions must be converted to the new format.",
            TreatAsErrorFromVersion = "6.0.0",
            RemoveInVersion = "7.0.0")]
        public static PersistenceExtensions<RavenDBPersistence> UseLegacyVersionedSubscriptions(this PersistenceExtensions<RavenDBPersistence> cfg)
        {
            throw new NotImplementedException("Subscriptions must be converted to the new format.");
        }
    }
}