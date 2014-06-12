namespace NServiceBus.Persistence
{
    using System;
    using NServiceBus.RavenDB;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.RavenDB.SessionManagement;
    using Raven.Client;

    public class RavenDB : PersistenceDefinition
    {
    }

    public static class RavenDbSettingsExtenstions
    {
        internal const string DocumentStoreSettingsKey = "RavenDbDocumentStore";
        internal const string DefaultConnectionParameters = "RavenDbConnectionParameters";
        internal const string SharedSessionSettingsKey = "RavenDbSharedSession";

        public static PersistenceConfiguration SetDefaultDocumentStore(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(DocumentStoreSettingsKey, documentStore);
            RavenUserInstaller.AddDocumentStore(documentStore);
            return cfg;
        }

        public static PersistenceConfiguration SetDefaultDocumentStore(this PersistenceConfiguration cfg, ConnectionParameters connectionParameters)
        {
            cfg.Config.Settings.Set(DefaultConnectionParameters, connectionParameters);
            // This will be registered with RavenUserInstaller once we initialize the document store object internally
            return cfg;
        }

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
