namespace NServiceBus.Persistence
{
    using NServiceBus.RavenDB;
    using Raven.Client;

    public class RavenDB : PersistenceDefinition
    {
    }

    public static class RavenDbSettingsExtenstions
    {
        public const string DocumentStoreSettingsKey = "RavenDbDocumentStore";
        public const string DefaultConnectionParameters = "RavenDbConnectionParameters";

        public static void SetDefaultDocumentStore(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(DocumentStoreSettingsKey, documentStore);
        }

        public static void SetDefaultDocumentStore(this PersistenceConfiguration cfg, ConnectionParameters connectionParameters)
        {
            cfg.Config.Settings.Set(DefaultConnectionParameters, connectionParameters);
        }
    }
}
