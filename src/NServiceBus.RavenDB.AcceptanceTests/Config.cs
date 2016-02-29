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
        documentStore = GetDocumentStore();

        configuration.UsePersistence<RavenDBPersistence>().DoNotSetupDatabasePermissions().SetDefaultDocumentStore(documentStore);

        Console.WriteLine("Created '{0}' database", documentStore.DefaultDatabase);

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        return DeleteDatabase(documentStore);
    }

    public static DocumentStore GetDocumentStore()
    {
        var databaseName = Guid.NewGuid().ToString();

        var documentStore = new DocumentStore
        {
            Url = "http://localhost:8083",
            DefaultDatabase = databaseName,
            ResourceManagerId = Guid.NewGuid() /* This is OK for ATT purposes */
        };

        documentStore.Initialize();

        return documentStore;
    }

    public static async Task DeleteDatabase(DocumentStore documentStore)
    {
        // Periodically the delete will throw an exception because Raven has the database locked
        // To solve this we have a retry loop with a delay
        var triesLeft = 3;

        while (--triesLeft > 0)
        {
            try
            {
                await documentStore.AsyncDatabaseCommands.GlobalAdmin.DeleteDatabaseAsync(documentStore.DefaultDatabase, true);
                break;
            }
            catch
            {
                if (triesLeft < 1)
                {
                    throw;
                }

                await Task.Delay(250);
            }
        }

        Console.WriteLine("Deleted '{0}' database", documentStore.DefaultDatabase);
    }

    DocumentStore documentStore;
}