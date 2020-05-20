namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;

    /// <summary>
    ///     Provides configurations methods for the Raven storages
    /// </summary>
    public static class RavenDbSettingsExtensions
    {
        /// <summary>
        ///     Configures the storages to use the given document store supplied
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">Document store managed by me as a user</param>
        /// <returns></returns>
        public static PersistenceExtensions<RavenDBPersistence> SetDefaultDocumentStore(this PersistenceExtensions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDefaultStore(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        ///     Configures the storages to use the given document store supplied
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        /// <returns></returns>
        public static PersistenceExtensions<RavenDBPersistence> SetDefaultDocumentStore(this PersistenceExtensions<RavenDBPersistence> cfg, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDefaultStore(cfg.GetSettings(), storeCreator);
            return cfg;
        }

        /// <summary>
        ///     Configures the storages to use the given document store supplied
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        /// <returns></returns>
        public static PersistenceExtensions<RavenDBPersistence> SetDefaultDocumentStore(this PersistenceExtensions<RavenDBPersistence> cfg, Func<ReadOnlySettings, IBuilder, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDefaultStore(cfg.GetSettings(), storeCreator);
            return cfg;
        }

        /// <summary>
        ///     Configures the persisters to connection to the server specified
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="connectionParameters">Connection details</param>
        /// <returns></returns>
        [ObsoleteEx(
            Message = "ConnectionParameters is no longer supported. Use an alternate overload and supply the fully configured IDocumentStore.",
            RemoveInVersion = "7.0.0",
            TreatAsErrorFromVersion = "6.0.0")]
        public static PersistenceExtensions<RavenDBPersistence> SetDefaultDocumentStore(this PersistenceExtensions<RavenDBPersistence> cfg, ConnectionParameters connectionParameters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Specifies the async session that the shared persisters (saga + outbox) that should be used. The lifecycle is controlled by
        ///     me
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="getAsyncSessionFunc">A func returning the async session to be used</param>
        /// <returns></returns>
        public static PersistenceExtensions<RavenDBPersistence> UseSharedAsyncSession(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSessionFunc)
        {
            if (getAsyncSessionFunc == null)
            {
                throw new ArgumentNullException(nameof(getAsyncSessionFunc));
            }
            cfg.GetSettings().Set(RavenDbStorageSession.SharedAsyncSession, getAsyncSessionFunc);
            return cfg;
        }

        /// <summary>
        ///     Specifies the mapping to use for when resolving the database name to use for each message.
        /// </summary>
        /// <param name="cfg">The configuration object.</param>
        /// <param name="convention">
        ///     The method referenced by a Func delegate for finding the database name for the specified
        ///     message.
        /// </param>
        /// <returns>The configuration object.</returns>
        public static PersistenceExtensions<RavenDBPersistence> SetMessageToDatabaseMappingConvention(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IDictionary<string,string>, string> convention)
        {
            cfg.GetSettings().Set(RavenDbStorageSession.MessageToDatabaseMappingConvention, convention);
            return cfg;
        }

        /// <summary>
        ///     Tells the persister to not setup user permissions for the database
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        [ObsoleteEx(
            Message = "Database permissions are no longer set up, so this method has no effect. All calls to this method may be safely removed.",
            RemoveInVersion = "7.0.0",
            TreatAsErrorFromVersion = "6.0.0")]
        public static PersistenceExtensions<RavenDBPersistence> DoNotSetupDatabasePermissions(this PersistenceExtensions<RavenDBPersistence> cfg)
        {
            throw new NotImplementedException();
        }
    }
}