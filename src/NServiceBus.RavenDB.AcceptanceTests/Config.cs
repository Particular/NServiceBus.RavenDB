using System;
using NServiceBus;
using NServiceBus.Persistence;
using Raven.Client.Document;

public class ConfigureRavenDBPersistence
{
    public void Configure(Configure config)
    {
        var store = new DocumentStore
        {
            Url = "http://localhost:8081",
            DefaultDatabase = Guid.NewGuid().ToString(),
        };

        store.Initialize();

        config.UsePersistence<RavenDB>(c =>
        {
            c.DoNotSetupDatabasePermissions();
            c.SetDefaultDocumentStore(store);
        });
    }
}