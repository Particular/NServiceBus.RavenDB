﻿using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Settings;
using System;
using System.Threading.Tasks;
using NServiceBus.Configuration.AdvancedExtensibility;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

public class ConfigureEndpointRavenDBPersistence : IConfigureEndpointTestExecution
{
    const string DefaultDocumentStoreKey = "$.ConfigureEndpointRavenDBPersistence.DefaultDocumentStore";
    const string DefaultPersistenceExtensionsKey = "$.ConfigureRavenDBPersistence.DefaultPersistenceExtensions";

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var documentStore = GetDocumentStore();

        databaseName = documentStore.Database;

        configuration.GetSettings().Set(DefaultDocumentStoreKey, documentStore);

        var persistenceExtensions = configuration.UsePersistence<RavenDBPersistence>()
            .DoNotCacheSubscriptions()
            .SetDefaultDocumentStore(documentStore);

        configuration.GetSettings().Set(DefaultPersistenceExtensionsKey, persistenceExtensions);

        Console.WriteLine("Created '{0}' database", documentStore.Database);

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        return DeleteDatabase(databaseName);
    }

    public static DocumentStore GetDocumentStore()
    {
        var dbName = Guid.NewGuid().ToString();

        var documentStore = GetInitializedDocumentStore(dbName);

        CreateDatabase(documentStore, dbName);

        return documentStore;
    }

    internal static DocumentStore GetInitializedDocumentStore(string defaultDatabase)
    {
        var urls = Environment.GetEnvironmentVariable("CommaSeparatedRavenClusterUrls") ?? "http://localhost:8080";

        var documentStore = new DocumentStore
        {
            Urls = urls.Split(','),
            Database = defaultDatabase
        };

        documentStore.Initialize();

        return documentStore;
    }

    public static void CreateDatabase(IDocumentStore defaultStore, string dbName)
    {
        var dbRecord = new DatabaseRecord(dbName);
        defaultStore.Maintenance.Server.Send(new CreateDatabaseOperation(dbRecord));
    }

    public static async Task DeleteDatabase(string dbName)
    {
        // Periodically the delete will throw an exception because Raven has the database locked
        // To solve this we have a retry loop with a delay
        var triesLeft = 3;

        while (triesLeft-- > 0)
        {
            try
            {
                // We are using a new store because the global one is disposed of before cleanup
                using (var storeForDeletion = GetInitializedDocumentStore(dbName))
                {
                    storeForDeletion.Maintenance.Server.Send(new DeleteDatabasesOperation(storeForDeletion.Database, hardDelete: true));
                    break;
                }
            }
            catch
            {
                if (triesLeft == 0)
                {
                    throw;
                }

                await Task.Delay(250);
            }
        }

        Console.WriteLine("Deleted '{0}' database", dbName);
    }

    string databaseName;

    public static DocumentStore GetDefaultDocumentStore(ReadOnlySettings settings)
    {
        return settings.Get<DocumentStore>(DefaultDocumentStoreKey);
    }

    public static PersistenceExtensions<RavenDBPersistence> GetDefaultPersistenceExtensions(ReadOnlySettings settings)
    {
        return settings.Get<PersistenceExtensions<RavenDBPersistence>>(DefaultPersistenceExtensionsKey);
    }
}

public static class TestConfigurationExtensions
{
    public static PersistenceExtensions<RavenDBPersistence> ResetDocumentStoreSettings(this PersistenceExtensions<RavenDBPersistence> cfg, out TestDatabaseInfo dbInfo)
    {
        var settings = cfg.GetSettings();
        var docStore = ConfigureEndpointRavenDBPersistence.GetDefaultDocumentStore(settings);

        settings.Set("RavenDbDocumentStore", null);
        dbInfo = new TestDatabaseInfo
        {
            Urls = docStore.Urls,
            Database = docStore.Database
        };
        return cfg;
    }
}

public class TestDatabaseInfo
{
    public string[] Urls { get; set; }
    public string Database { get; set; }
}
