namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.Settings;
    using Raven.Client;

    /// <summary>
    ///     Provides configuration options specific to the subscription storage
    /// </summary>
    public static class RavenDbSubscriptionSettingsExtensions
    {
        /// <summary>
        ///     Configures the given document store to be used when storing subscriptions
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">The document store to use</param>
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForSubscriptions(this PersistenceExtentions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Subscriptions>(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        ///     Configures the given document store to be used when storing subscriptions
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForSubscriptions(this PersistenceExtentions<RavenDBPersistence> cfg, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Subscriptions>(cfg.GetSettings(), storeCreator);
            return cfg;
        }
    }
}