namespace NServiceBus
{
    using System;
    using RavenDB;
    using RavenDB.Persistence;
    using RavenDB.SessionManagement;
    using Persistence;
    using Raven.Client;

    /// <summary>
    /// Provides configurations methods for the Raven storages
    /// </summary>
    public static class RavenDbSettingsExtensions
    {
        internal const string DocumentStoreSettingsKey = "RavenDbDocumentStore";
        internal const string DefaultConnectionParameters = "RavenDbConnectionParameters";
        internal const string SharedSessionSettingsKey = "RavenDbSharedSession";

        /// <summary>
        /// Configures the storages to use the given document store supplied
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">Document store managed by me as a user</param>
        /// <returns></returns>
        public static PersistenceConfiguration SetDefaultDocumentStore(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(DocumentStoreSettingsKey, documentStore);
            RavenUserInstaller.AddDocumentStore(documentStore);
            return cfg;
        }

        /// <summary>
        /// Configures the persisters to connection to the server specified
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="connectionParameters">Connection details</param>
        /// <returns></returns>
        public static PersistenceConfiguration SetDefaultDocumentStore(this PersistenceConfiguration cfg, ConnectionParameters connectionParameters)
        {
            cfg.Config.Settings.Set(DefaultConnectionParameters, connectionParameters);
            // This will be registered with RavenUserInstaller once we initialize the document store object internally
            return cfg;
        }

        /// <summary>
        /// Specifies the session that the shared persisters (saga + outbox) that should be used. The lifecycle is controled by me
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="getSessionFunc">A func returning the session to be used</param>
        /// <returns></returns>
        public static PersistenceConfiguration UseSharedSession(this PersistenceConfiguration cfg, Func<IDocumentSession> getSessionFunc)
        {
            cfg.Config.Settings.Set(SharedSessionSettingsKey, getSessionFunc);
            return cfg;
        }

        /// <summary>
        /// Specifies the mapping to use for when resolving the database name to use for each message.
        /// </summary>
        /// <param name="cfg">The configuration object.</param>
        /// <param name="convention">The method referenced by a Func delegate for finding the database name for the specified message.</param>
        /// <returns>The configuration object.</returns>
        public static PersistenceConfiguration SetMessageToDatabaseMappingConvention(this PersistenceConfiguration cfg, Func<IMessageContext, string> convention)
        {
            OpenSessionBehavior.GetDatabaseName = convention;
            return cfg;
        }
    }
}