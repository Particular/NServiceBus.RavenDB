using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Settings;
using Raven.Client;
using Raven.Client.Document;

public class ConfigureScenariosForRavenDBPersistence : IConfigureSupportedScenariosForTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes => new List<Type>();
}

public class ConfigureEndpointRavenDBPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        documentStore = new DocumentStore
        {
            Url = "http://localhost:8083",
            DefaultDatabase = Guid.NewGuid().ToString()
        };

        var endpointSettings = configuration.GetSettings();

        endpointSettings.Set(DefaultDocumentStoreKey, documentStore);

        var persistenceExtensions = configuration.UsePersistence<RavenDBPersistence>()
            .DoNotSetupDatabasePermissions()
            .SetDefaultDocumentStore(documentStore)
            .SetTransactionRecoveryStorageBasePath("%LOCALAPPDATA%");

        endpointSettings.Set(DefaultPersistenceExtensionsKey, persistenceExtensions);

        Console.WriteLine("Created '{0}' database", documentStore.DefaultDatabase);

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        return DeleteDatabase(documentStore);
    }

    private static async Task DeleteDatabase(DocumentStore documentStore)
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

    public static IDocumentStore GetDefaultDocumentStore(ReadOnlySettings settings)
    {
        return settings.Get<IDocumentStore>(DefaultDocumentStoreKey);
    }

    public static PersistenceExtentions<RavenDBPersistence> GetDefaultPersistenceExtensions(ReadOnlySettings settings)
    {
        return settings.Get<PersistenceExtentions<RavenDBPersistence>>(DefaultPersistenceExtensionsKey);
    }

    DocumentStore documentStore;
    const string DefaultDocumentStoreKey = "$.ConfigureRavenDBPersistence.DefaultDocumentStore";
    const string DefaultPersistenceExtensionsKey = "$.ConfigureRavenDBPersistence.DefaultPersistenceExtensions";
}