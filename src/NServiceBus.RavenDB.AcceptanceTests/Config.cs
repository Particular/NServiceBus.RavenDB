using System;
using NServiceBus;
using NServiceBus.Persistence;
using Raven.Client.Document;

public class ConfigureRavenDBPersistence
{
    DocumentStore documentStore;

    public void Configure(BusConfiguration config)
    {
        documentStore = new DocumentStore
        {
            Url = "http://localhost:8081",
            DefaultDatabase = Guid.NewGuid().ToString(),
            ResourceManagerId = Guid.NewGuid() /* This is OK for ATT purposes */
        };

        documentStore.Initialize();

        config.UsePersistence<RavenDBPersistence>().DoNotSetupDatabasePermissions().SetDefaultDocumentStore(documentStore);

        Console.WriteLine("Created '{0}' database", documentStore.DefaultDatabase);
    }

    public void Cleanup()
    {
        documentStore.DatabaseCommands.GlobalAdmin.DeleteDatabase(documentStore.DefaultDatabase, hardDelete: true);

        Console.WriteLine("Deleted '{0}' database", documentStore.DefaultDatabase);
    }
}