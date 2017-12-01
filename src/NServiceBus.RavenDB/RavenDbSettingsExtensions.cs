namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;
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
        ///     Configures the persisters to connection to the server specified
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="connectionParameters">Connection details</param>
        /// <returns></returns>
        public static PersistenceExtensions<RavenDBPersistence> SetDefaultDocumentStore(this PersistenceExtensions<RavenDBPersistence> cfg, ConnectionParameters connectionParameters)
        {
            if (connectionParameters == null)
            {
                throw new ArgumentNullException(nameof(connectionParameters));
            }
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
        [ObsoleteEx(Message = "Use the 'UseSharedAsyncSession' configuration extension method to provide an async session.", RemoveInVersion = "5", TreatAsErrorFromVersion = "4")]
        public static PersistenceExtensions<RavenDBPersistence> UseSharedSession(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IDocumentSession> getSessionFunc)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Specifies the async session that the shared persisters (saga + outbox) that should be used. The lifecycle is controlled by
        ///     me
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="getAsyncSessionFunc">A func returning the async session to be used</param>
        /// <returns></returns>
        [ObsoleteEx(Message = "This overload doesn't provide enough information to the Func to do anything useful. Use the overload that supplies the message headers as an input instead.",
            TreatAsErrorFromVersion = "5.0.0", RemoveInVersion = "6.0.0")]
        public static PersistenceExtensions<RavenDBPersistence> UseSharedAsyncSession(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IAsyncDocumentSession> getAsyncSessionFunc)
        {
            if (getAsyncSessionFunc == null)
            {
                throw new ArgumentNullException(nameof(getAsyncSessionFunc));
            }
            cfg.GetSettings().Set(SharedAsyncSessionSettingsKey + ".Obsolete", getAsyncSessionFunc);
            return cfg;
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
        public static PersistenceExtensions<RavenDBPersistence> SetMessageToDatabaseMappingConvention(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IDictionary<string,string>, string> convention)
        {
            cfg.GetSettings().Set("RavenDB.SetMessageToDatabaseMappingConvention", convention);
            return cfg;
        }

        /// <summary>
        ///     Tells the persister to not setup user permissions for the database
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        public static PersistenceExtensions<RavenDBPersistence> DoNotSetupDatabasePermissions(this PersistenceExtensions<RavenDBPersistence> cfg)
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
        public static PersistenceExtensions<RavenDBPersistence> IConfirmToUseAStorageEngineWhichDoesntSupportDtcWhilstLeavingDistributedTransactionSupportEnabled(this PersistenceExtensions<RavenDBPersistence> cfg)
        {
            cfg.GetSettings().Set("RavenDB.IConfirmToUseAStorageEngineWhichDoesntSupportDtcWhilstLeavingDistributedTransactionSupportEnabled", true);
            return cfg;
        }
    }
}