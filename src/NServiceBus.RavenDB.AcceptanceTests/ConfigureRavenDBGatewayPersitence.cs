namespace NServiceBus.RavenDB.AcceptanceTests
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Gateway.AcceptanceTests;

    public class ConfigureRavenDBGatewayPersitence : IConfigureGatewayPersitenceExecution
    {
        public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
        {
            var documentStore = ConfigureEndpointRavenDBPersistence.GetDocumentStore();

            databaseName = documentStore.Database;


#pragma warning disable 618
            configuration.UsePersistence<RavenDBPersistence, StorageType.GatewayDeduplication>()
#pragma warning restore 618
                .DoNotSetupDatabasePermissions()
                .SetDefaultDocumentStore(documentStore);

            return Task.FromResult(0);
        }

        public Task Cleanup()
        {
            return ConfigureEndpointRavenDBPersistence.DeleteDatabase(databaseName);
        }

        string databaseName;
    }
}