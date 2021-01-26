﻿namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Threading;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;

    class ReusableDB : IDisposable
    {
        readonly string databaseName;
        readonly bool deleteOnCompletion;

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

                var dbRecord = new DatabaseRecord(databaseName);
                initStore.Maintenance.Server.Send(new CreateDatabaseOperation(dbRecord));
            }

            Console.WriteLine($"Provisioned new Raven database name {databaseName}");
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

        IDocumentStore CreateStore()
        {
            return new DocumentStore
            {
                Urls = TestConstants.RavenUrls,
                Database = databaseName
            };
        }

        public void WaitForIndexing(IDocumentStore store)
        {
            while (store.Maintenance.Send(new GetStatisticsOperation()).StaleIndexes.Length != 0)
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
