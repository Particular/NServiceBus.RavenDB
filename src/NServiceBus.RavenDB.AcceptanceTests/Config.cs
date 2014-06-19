using NServiceBus;
using NServiceBus.Persistence;
using Raven.Client.Embedded;

public class ConfigureRavenDBPersistence
{
    public void Configure(Configure config)
    {
        var store = new EmbeddableDocumentStore
        {
            RunInMemory = true
        };

        store.Initialize();

        config.UsePersistence<RavenDB>(c =>
        {
            c.DoNotSetupDatabasePermissions();
            c.SetDefaultDocumentStore(store);
        });
    }
}