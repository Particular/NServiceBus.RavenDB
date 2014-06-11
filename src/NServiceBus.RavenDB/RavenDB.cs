namespace NServiceBus.Persistence
{
    using System;
    using NServiceBus.RavenDB;
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
            return cfg;
        }

        public static PersistenceConfiguration SetDefaultDocumentStore(this PersistenceConfiguration cfg, ConnectionParameters connectionParameters)
        {
            cfg.Config.Settings.Set(DefaultConnectionParameters, connectionParameters);
            return cfg;
        }

        public static PersistenceConfiguration UseSharedSession(this PersistenceConfiguration cfg, Func<IDocumentSession> getSessionFunc)
        {
            cfg.Config.Settings.Set(SharedSessionSettingsKey, getSessionFunc);
            return cfg;
        }
    }
}
