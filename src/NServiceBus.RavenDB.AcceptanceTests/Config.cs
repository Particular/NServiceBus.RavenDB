using System;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence;
using NServiceBus.Settings;
using Raven.Client;
using Raven.Client.Document;

public class ConfigureRavenDBPersistence
{
    DocumentStore documentStore;

    const string DefaultDocumentStoreKey = "$.ConfigureRavenDBPersistence.DefaultDocumentStore";
    const string DefaultPersistenceExtensionsKey = "$.ConfigureRavenDBPersistence.DefaultPersistenceExtensions";

    public void Configure(BusConfiguration config)
    {
        documentStore = new DocumentStore
        {
            Url = "http://localhost:8083",
            DefaultDatabase = Guid.NewGuid().ToString()
        };

        var settings = config.GetSettings();
        
        settings.Set(DefaultDocumentStoreKey, documentStore);

        var persistenceExtensions = config.UsePersistence<RavenDBPersistence>()
            .DoNotSetupDatabasePermissions()
            .SetDefaultDocumentStore(documentStore)
            .SetTransactionRecoveryStorageBasePath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

        settings.Set(DefaultPersistenceExtensionsKey, persistenceExtensions);

        Console.WriteLine("Created '{0}' database", documentStore.DefaultDatabase);
    }

    public void Cleanup()
    {
        documentStore.DatabaseCommands.GlobalAdmin.DeleteDatabase(documentStore.DefaultDatabase, hardDelete: true);

        Console.WriteLine("Deleted '{0}' database", documentStore.DefaultDatabase);
    }

    public static IDocumentStore GetDefaultDocumentStore(ReadOnlySettings settings)
    {
        return settings.Get<IDocumentStore>(DefaultDocumentStoreKey);
    }

    public static PersistenceExtentions<RavenDBPersistence> GetDefaultPersistenceExtensions(ReadOnlySettings settings)
    {
        return settings.Get<PersistenceExtentions<RavenDBPersistence>>(DefaultPersistenceExtensionsKey);
    }
}