namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;
    using Raven.Client;

    /// <summary>
    ///     Configuration settings specific to the timeout storage
    /// </summary>
    public static class RavenDbTimeoutSettingsExtensions
    {
        /// <summary>
        ///     Configures the given document store to be used when storing timeouts
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">The document store to use</param>
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForTimeouts(this PersistenceExtentions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Timeouts>(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        ///     Configures the given document store to be used when storing timeouts
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        public static PersistenceExtentions<RavenDBPersistence> UseDocumentStoreForTimeouts(this PersistenceExtentions<RavenDBPersistence> cfg, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Timeouts>(cfg.GetSettings(), storeCreator);
            return cfg;
        }
    }
}