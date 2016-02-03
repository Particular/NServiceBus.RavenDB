using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using Raven.Client.Document;

public class ConfigureRavenDBPersistence : IConfigureTestExecution
{
    public Task Configure(BusConfiguration configuration, IDictionary<string, string> settings)
    {
        documentStore = GetDocumentStore();

        configuration.UsePersistence<RavenDBPersistence>().DoNotSetupDatabasePermissions().SetDefaultDocumentStore(documentStore);

        Console.WriteLine("Created '{0}' database", documentStore.DefaultDatabase);

        return Task.FromResult(0);
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

    public async Task Cleanup()
    {
        await DeleteDatabase(documentStore);
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