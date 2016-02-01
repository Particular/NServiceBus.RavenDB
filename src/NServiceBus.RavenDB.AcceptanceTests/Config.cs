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
        var databaseName = Guid.NewGuid().ToString();
        documentStore = new DocumentStore
        {
            Url = "http://localhost:8083",
            DefaultDatabase = databaseName,
            ResourceManagerId = Guid.NewGuid() /* This is OK for ATT purposes */
        };

        documentStore.Initialize();

        configuration.UsePersistence<RavenDBPersistence>().DoNotSetupDatabasePermissions().SetDefaultDocumentStore(documentStore);

        Console.WriteLine("Created '{0}' database", documentStore.DefaultDatabase);

        return Task.FromResult(0);
    }

    public async Task Cleanup()
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
                    throw;

                await Task.Delay(100);
            }
        }

        Console.WriteLine("Deleted '{0}' database", documentStore.DefaultDatabase);
    }

    DocumentStore documentStore;
}