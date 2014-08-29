namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using RavenDB;
    using RavenDB.SessionManagement;
    using Raven.Client;

    /// <summary>
    /// Provides configurations methods for the Raven storages
    /// </summary>
    public static class RavenDbSettingsExtensions
    {
        internal const string DocumentStoreSettingsKey = "RavenDbDocumentStore";
        internal const string DefaultConnectionParameters = "RavenDbConnectionParameters";
        internal const string SharedSessionSettingsKey = "RavenDbSharedSession";
        internal const string UseLegacyRavenDbConfigs = "RavenDB.UseLegacyConfigs";

        /// <summary>
        /// Configures the storages to use the given document store supplied
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">Document store managed by me as a user</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> SetDefaultDocumentStore(this PersistenceExtentions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            cfg.GetSettings().Set(DocumentStoreSettingsKey, documentStore);
            return cfg;
        }

        /// <summary>
        /// Configures the persisters to connection to the server specified
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
        /// Specifies the session that the shared persisters (saga + outbox) that should be used. The lifecycle is controled by me
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="getSessionFunc">A func returning the session to be used</param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> UseSharedSession(this PersistenceExtentions<RavenDBPersistence> cfg, Func<IDocumentSession> getSessionFunc)
        {
            cfg.GetSettings().Set(SharedSessionSettingsKey, getSessionFunc);
            return cfg;
        }

        /// <summary>
        /// Specifies the mapping to use for when resolving the database name to use for each message.
        /// </summary>
        /// <param name="cfg">The configuration object.</param>
        /// <param name="convention">The method referenced by a Func delegate for finding the database name for the specified message.</param>
        /// <returns>The configuration object.</returns>
        public static PersistenceExtentions<RavenDBPersistence> SetMessageToDatabaseMappingConvention(this PersistenceExtentions<RavenDBPersistence> cfg, Func<IMessageContext, string> convention)
        {
            OpenSessionBehavior.GetDatabaseName = convention;
            return cfg;
        }

        /// <summary>
        /// Tells the persister to not setup user permissions for the database
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> DoNotSetupDatabasePermissions(this PersistenceExtentions<RavenDBPersistence> cfg)
        {
            cfg.GetSettings().Set("RavenDB.DoNotSetupPermissions", true);
            return cfg;
        }

        /// <summary>
        /// Use legacy NServiceBus configs (versions 3 and 4). Those configs are deprecated, off by default and are not
        /// recommended for use. Only explicitly enable them during an upgrade process or if your system is known to
        /// rely on any of them.
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        public static PersistenceExtentions<RavenDBPersistence> UseLegacySettings(this PersistenceExtentions<RavenDBPersistence> cfg)
        {
            cfg.GetSettings().Set(UseLegacyRavenDbConfigs, true);
            return cfg;
        }
    }
}