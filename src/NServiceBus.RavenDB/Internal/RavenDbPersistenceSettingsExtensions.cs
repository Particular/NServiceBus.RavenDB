namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using NServiceBus.RavenDB.Internal;
    using Raven.Client;

    /// <summary>
    ///     Configuration settings specific to the timeout storage
    /// </summary>
    public static class RavenDbPersistenceSettingsExtensions
    {
        internal const string TransactionRecoveryStorageBasePathKey = "RavenDB/TransactionRecoveryStorage/BasePath";

        /// <summary>
        /// Run a delegate against the RavenDB DocumentStore used by NServiceBus before NServiceBus calls Initialize on it.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="customize">Will be run on the DocumentStore just prior to Initialize() being called.</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> CustomizeDocumentStore(this PersistenceExtentions<RavenDBPersistence> cfg, Action<IDocumentStore> customize)
        {
            DocumentStoreManager.SetCustomizeDocumentStoreDelegate(cfg.GetSettings(), customize);
            return cfg;
        }

        /// <summary>
        /// Specify a location where RavenDB can store transaction recovery information. The application must have write permissions to the directory.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="basePath">A directory where the application will have read/write permisisons.</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> SetTransactionRecoveryStorageBasePath(this PersistenceExtentions<RavenDBPersistence> cfg, string basePath)
        {
            cfg.GetSettings().Set(TransactionRecoveryStorageBasePathKey, basePath);
            return cfg;
        }
    }
}