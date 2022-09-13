namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;

    [SetUpFixture]
    public class SetupFixture
    {
        public static string DefaultDatabaseName { get; private set; }
        public static string TenantId { get; private set; }
        public static DocumentStore DocumentStore { get; private set; }

        [OneTimeSetUp]
        public async Task Setup()
        {
            DefaultDatabaseName = Guid.NewGuid().ToString("N");
            TenantId = $"{DefaultDatabaseName}-tenant-1";

            DocumentStore = new DocumentStore
            {
                Database = DefaultDatabaseName,
                Urls = new[] { GetConnectionString() }
            };
            DocumentStore.Initialize();

            await DocumentStore.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(DefaultDatabaseName)));
            await DocumentStore.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(TenantId)));
        }

        [OneTimeTearDown]
        public async Task Teardown() =>
            await DocumentStore.Maintenance.Server.SendAsync(new DeleteDatabasesOperation(new DeleteDatabasesOperation.Parameters
            {
                DatabaseNames = new[] { DefaultDatabaseName, TenantId }
            }));

        static string GetConnectionString() => Environment.GetEnvironmentVariable("RavenSingleNodeUrl") ?? "http://localhost:8080";
    }
}