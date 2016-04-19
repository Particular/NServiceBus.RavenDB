namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Threading;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Indexes;

    class ReusableDB : IDisposable
    {
        private readonly string databaseName;
        private readonly bool deleteOnCompletion;

        public ReusableDB(bool deleteOnCompletion = true)
        {
            this.deleteOnCompletion = deleteOnCompletion;
            databaseName = Guid.NewGuid().ToString("N");

            // The Raven client does this as a courtesy but may fail. During tests a race condition could
            // prevent it from existing in time. So we are forcing the issue. In real life, every connection
            // to the server ever attempting to ensure its existence means we can rely on it.
            using (var initStore = CreateStore())
            {
                initStore.Initialize();
                new RavenDocumentsByEntityName().Execute(initStore);
            }

            Console.WriteLine($"Provisioned new Raven database name {databaseName}");
        }

        public IDocumentStore NewStore()
        {
            Console.WriteLine();
            Console.WriteLine($"Creating new DocumentStore for {databaseName}");
            return CreateStore();
        }

        private IDocumentStore CreateStore()
        {
            return new DocumentStore
            {
                Url = "http://localhost:8083",
                DefaultDatabase = databaseName
            };
        }

        public void WaitForIndexing(IDocumentStore store)
        {
            while (store.DatabaseCommands.GetStatistics().StaleIndexes.Length != 0)
            {
                Thread.Sleep(250);
            }

            Thread.Sleep(100);
        }

        public void Dispose()
        {
            if (!deleteOnCompletion)
            {
                return;
            }

            var docStore = new DocumentStore
            {
                Url = "http://localhost:8083"
            };

            docStore.Initialize();

            for (var i = 0; i < 4; i++)
            {
                try
                {
                    docStore.DatabaseCommands.GlobalAdmin.DeleteDatabase(databaseName, hardDelete: true);
                    return;
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to delete database, waiting 250ms");
                    Thread.Sleep(250);
                }
            }

            docStore.DatabaseCommands.GlobalAdmin.DeleteDatabase(databaseName, hardDelete: true);
        }
    }
}
