namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.RavenDB;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.RavenDB.SessionManagement;
    using Raven.Client;

    /// <summary>
    ///     Provides configurations methods for the Raven storages
    /// </summary>
    public static class RavenDbSettingsExtensions
    {
        internal const string DefaultConnectionParameters = "RavenDbConnectionParameters";
        internal const string SharedAsyncSessionSettingsKey = "RavenDbSharedAsyncSession";

        /// <summary>
        ///     Configures the storages to use the given document store supplied
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">Document store managed by me as a user</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> SetDefaultDocumentStore(this PersistenceExtentions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDefaultStore(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        ///     Configures the persisters to connection to the server specified
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="connectionParameters">Connection details</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> SetDefaultDocumentStore(this PersistenceExtentions<RavenDBPersistence> cfg, ConnectionParameters connectionParameters)
        {
            cfg.GetSettings().Set(DefaultConnectionParameters, connectionParameters);
            // This will be registered with RavenUserInstaller once we initialize the document store object internally
            return cfg;
        }

        /// <summary>
        ///     Specifies the session that the shared persisters (saga + outbox) that should be used. The lifecycle is controled by
        ///     me
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="getSessionFunc">A func returning the session to be used</param>
        /// <returns></returns>
        [ObsoleteEx( Message = "Use the 'UseSharedAsyncSession' configuration extension method to provide an async session.", RemoveInVersion = "5", TreatAsErrorFromVersion = "4" )]
        public static PersistenceExtentions<RavenDBPersistence> UseSharedSession(this PersistenceExtentions<RavenDBPersistence> cfg, Func<IDocumentSession> getSessionFunc)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Specifies the async session that the shared persisters (saga + outbox) that should be used. The lifecycle is controled by
        ///     me
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="getAsyncSessionFunc">A func returning the async session to be used</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> UseSharedAsyncSession( this PersistenceExtentions<RavenDBPersistence> cfg, Func<IAsyncDocumentSession> getAsyncSessionFunc )
        {
            cfg.GetSettings().Set(SharedAsyncSessionSettingsKey, getAsyncSessionFunc);
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
     //todo: obsolete
        public static PersistenceExtentions<RavenDBPersistence> SetMessageToDatabaseMappingConvention(this PersistenceExtentions<RavenDBPersistence> cfg, Func<IDictionary<string,string>, string> convention)
        {
            OpenAsyncSessionBehavior.GetDatabaseName = convention;
            return cfg;
        }

        /// <summary>
        ///     Tells the persister to not setup user permissions for the database
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> DoNotSetupDatabasePermissions(this PersistenceExtentions<RavenDBPersistence> cfg)
        {
            cfg.GetSettings().Set("RavenDB.DoNotSetupPermissions", true);
            return cfg;
        }

        /// <summary>
        ///     Confirms the usage of a storage engine (i.ex. voron) which doesn't support distributed transactions
        ///     whilst leaving the distributed transaction support enabled.
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> IConfirmToUseAStorageEngineWhichDoesntSupportDtcWhilstLeavingDistributedTransactionSupportEnabled(this PersistenceExtentions<RavenDBPersistence> cfg)
        {
            cfg.GetSettings().Set("RavenDB.IConfirmToUseAStorageEngineWhichDoesntSupportDtcWhilstLeavingDistributedTransactionSupportEnabled", true);
            return cfg;
        }
    }
}