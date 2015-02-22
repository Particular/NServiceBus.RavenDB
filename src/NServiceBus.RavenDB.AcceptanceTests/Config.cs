using System;
using NServiceBus;
using NServiceBus.Persistence;
using Raven.Client.Document;

public class ConfigureRavenDBPersistence
{
    public void Configure(BusConfiguration config)
    {
        var store = new DocumentStore
        {
            Url = "http://localhost:8083",
            DefaultDatabase = Guid.NewGuid().ToString("N").Substring(0, 8),
        };

        store.Initialize();

        config.UsePersistence<RavenDBPersistence>().DoNotSetupDatabasePermissions().SetDefaultDocumentStore(store);
    }
}