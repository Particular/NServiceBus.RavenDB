namespace NServiceBus.Persistence
{
    using Raven.Client;

    public class RavenDB : PersistenceDefinition
    {
    }

    public static class RavenDbSettingsExtenstions
    {
        public const string DocumentStoreSettingsKey = "RavenDbDocumentStore";

        public static void SetDefaultDocumentStore(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(DocumentStoreSettingsKey, documentStore);
        }
    }
}
