namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations;
    using Raven.Client.ServerWide.Operations;

    partial class ReusableDB : IReusableDB, IDisposable
    {
        readonly string databaseName;
        readonly bool deleteOnCompletion;

        public ReusableDB(bool deleteOnCompletion = true)
        {
            this.deleteOnCompletion = deleteOnCompletion;
            databaseName = Guid.NewGuid().ToString("N");
        }

        public IDocumentStore NewStore(string identifier = null)
        {
            Console.WriteLine();
            Console.WriteLine($"Creating new DocumentStore for {databaseName}");
            var store = CreateStore();

            if (identifier != null)
            {
                store.Identifier = identifier;
            }

            return store;
        }

        public IDocumentStore CreateStore() =>
            new DocumentStore
            {
                Urls = TestConstants.RavenUrls,
                Database = databaseName
            };

        public async Task WaitForIndexing(IDocumentStore store, CancellationToken cancellationToken = default)
        {
            while ((await store.Maintenance.SendAsync(new GetStatisticsOperation(), cancellationToken)).StaleIndexes.Length != 0)
            {
                await Task.Delay(250, cancellationToken);
            }

            await Task.Delay(100, cancellationToken);
        }

        public void Dispose()
        {
            if (!deleteOnCompletion)
            {
                return;
            }

            var docStore = new DocumentStore
            {
                Urls = TestConstants.RavenUrls
            };

            docStore.Initialize();

            for (var i = 0; i < 4; i++)
            {
                try
                {
                    docStore.Maintenance.Server.Send(new DeleteDatabasesOperation(databaseName, hardDelete: true));
                    return;
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to delete database, waiting 250ms");
                    Thread.Sleep(250);
                }
            }

            docStore.Maintenance.Server.Send(new DeleteDatabasesOperation(databaseName, hardDelete: true));
        }
    }
}
