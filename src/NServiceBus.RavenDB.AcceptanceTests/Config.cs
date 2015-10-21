using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence;
using Raven.Client.Document;

public class ConfigureRavenDBPersistence
{
    public Task Configure(BusConfiguration config)
    {
        var databaseName = Guid.NewGuid().ToString();
        documentStore = new DocumentStore
        {
            Url = "http://localhost:8083",
            DefaultDatabase = databaseName,
            ResourceManagerId = Guid.NewGuid() /* This is OK for ATT purposes */
        };

        documentStore.Initialize();

        documentStore.DatabaseCommands.GlobalAdmin.EnsureDatabaseExists(databaseName);

        config.UsePersistence<RavenDBPersistence>().DoNotSetupDatabasePermissions().SetDefaultDocumentStore(documentStore);

        Console.WriteLine("Created '{0}' database", documentStore.DefaultDatabase);

        return Task.FromResult(0);
    }

    public async Task Cleanup()
    {
        await documentStore.AsyncDatabaseCommands.GlobalAdmin.DeleteDatabaseAsync(documentStore.DefaultDatabase, true);

        Console.WriteLine("Deleted '{0}' database", documentStore.DefaultDatabase);
    }

    DocumentStore documentStore;
}