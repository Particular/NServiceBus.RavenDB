using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Settings;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

public class ConfigureEndpointRavenDBPersistence : IConfigureEndpointTestExecution
{
    const string DefaultDocumentStoreKey = "$.ConfigureEndpointRavenDBPersistence.DefaultDocumentStore";
    const string DefaultPersistenceExtensionsKey = "$.ConfigureRavenDBPersistence.DefaultPersistenceExtensions";

    public async Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var documentStore = await GetDocumentStore();

        databaseName = documentStore.Database;

        configuration.GetSettings().Set(DefaultDocumentStoreKey, documentStore);

        var persistenceExtensions = configuration.UsePersistence<RavenDBPersistence>()
            .DoNotCacheSubscriptions()
            .SetDefaultDocumentStore(documentStore);

        persistenceExtensions.Sagas().UseOptimisticLocking();

        configuration.GetSettings().Set(DefaultPersistenceExtensionsKey, persistenceExtensions);

        Console.WriteLine("Created '{0}' database", documentStore.Database);
    }

    public Task Cleanup() => DeleteDatabase(databaseName);

    public static async Task<DocumentStore> GetDocumentStore()
    {
        var dbName = Guid.NewGuid().ToString("N");

        var documentStore = GetInitializedDocumentStore(dbName);

        await CreateDatabase(documentStore, dbName);

        return documentStore;
    }

    internal static DocumentStore GetInitializedDocumentStore(string defaultDatabase)
    {
        var urls = Environment.GetEnvironmentVariable("RavenSingleNodeUrl") ?? "http://localhost:8080";

        var documentStore = new DocumentStore
        {
            Urls = urls.Split(','),
            Database = defaultDatabase
        };

        documentStore.Initialize();

        return documentStore;
    }

    public static Task CreateDatabase(IDocumentStore defaultStore, string dbName, CancellationToken cancellationToken = default)
    {
        var dbRecord = new DatabaseRecord(dbName);
        return defaultStore.Maintenance.Server.SendAsync(new CreateDatabaseOperation(dbRecord), cancellationToken);
    }

    public static async Task DeleteDatabase(string dbName, CancellationToken cancellationToken = default)
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
                    await storeForDeletion.Maintenance.Server.SendAsync(new DeleteDatabasesOperation(storeForDeletion.Database, hardDelete: true), cancellationToken);
                    break;
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) || !cancellationToken.IsCancellationRequested)
            {
                if (triesLeft == 0)
                {
                    throw;
                }

                await Task.Delay(250, cancellationToken);
            }
        }

        Console.WriteLine("Deleted '{0}' database", dbName);
    }

    string databaseName;

    public static DocumentStore GetDefaultDocumentStore(IReadOnlySettings settings)
        => settings.Get<DocumentStore>(DefaultDocumentStoreKey);

    public static PersistenceExtensions<RavenDBPersistence> GetDefaultPersistenceExtensions(IReadOnlySettings settings)
        => settings.Get<PersistenceExtensions<RavenDBPersistence>>(DefaultPersistenceExtensionsKey);
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
