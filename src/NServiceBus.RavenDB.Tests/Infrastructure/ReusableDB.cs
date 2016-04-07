namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Threading;
    using Raven.Client;
    using Raven.Client.Document;

    class ReusableDB : IDisposable
    {
        private readonly string databaseName;
        private readonly bool deleteOnCompletion;

        public ReusableDB(bool deleteOnCompletion = true)
        {
            this.deleteOnCompletion = deleteOnCompletion;
            databaseName = Guid.NewGuid().ToString("N");

            Console.WriteLine($"Provisioned new Raven database name {databaseName}");
        }

        public IDocumentStore NewStore()
        {
            Console.WriteLine();
            Console.WriteLine($"Creating new DocumentStore for {databaseName}");
            return new DocumentStore
            {
                Url = "http://localhost:8081",
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
                Url = "http://localhost:8081"
            };

            docStore.Initialize();

            for (var i = 0; i < 4; i++)
            {
                try
                {
                    DeleteDatabase(docStore, databaseName);
                    return;
                }
                catch (Exception x)
                {
                    Console.WriteLine("Unable to delete database, waiting 250ms: {0}", x.Message);
                    Thread.Sleep(250);
                }
            }

            DeleteDatabase(docStore, databaseName);
        }

        private void DeleteDatabase(IDocumentStore store, string databaseName)
        {
            var client = store.AsyncDatabaseCommands.ForSystemDatabase();

            var deleteUrl = string.Format("/admin/databases/{0}?hard-delete=true", Uri.EscapeDataString(databaseName));

            var request = client.CreateRequest(deleteUrl, "DELETE");
            request.ExecuteRequestAsync().Wait();

            Console.WriteLine("Deleted '{0}' database", databaseName);
        }
    }
}
