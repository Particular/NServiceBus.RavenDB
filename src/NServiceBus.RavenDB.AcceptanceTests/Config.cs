using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using Raven.Client.Document;

public class ConfigureScenariosForRavenDBPersistence : IConfigureSupportedScenariosForTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes => new List<Type>();
}

public class ConfigureEndpointRavenDBPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        var documentStore = GetDocumentStore();

        databaseName = documentStore.DefaultDatabase;

        configuration.UsePersistence<RavenDBPersistence>().DoNotSetupDatabasePermissions().SetDefaultDocumentStore(documentStore);

        Console.WriteLine("Created '{0}' database", documentStore.DefaultDatabase);

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

        return documentStore;
    }

    private static DocumentStore GetInitializedDocumentStore(string defaultDatabase)
    {
        var documentStore = new DocumentStore
        {
            Url = "http://localhost:8083",
            DefaultDatabase = defaultDatabase,
            ResourceManagerId = Guid.NewGuid() /* This is OK for ATT purposes */
        };

        documentStore.Initialize();

        return documentStore;
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
                    await storeForDeletion.AsyncDatabaseCommands.GlobalAdmin.DeleteDatabaseAsync(dbName, true);
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
}