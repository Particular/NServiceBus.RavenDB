namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using Raven.Abstractions.Data;
    using Raven.Client;

    class OutboxRecordsCleaner
    {
        volatile bool doingCleanup;
        IDocumentStore documentStore;
        string indexName;

        public OutboxRecordsCleaner(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
            indexName = new OutboxRecordsIndex().IndexName;
        }

        public void RemoveEntriesOlderThan(DateTime dateTime)
        {
            lock (this)
            {
                if (doingCleanup)
                {
                    return;
                }

                doingCleanup = true;
            }

            try
            {
                var query = new IndexQuery
                {
                    Query = $"Dispatched:true AND DispatchedAt:[* TO {dateTime:o}]"
                };

                var bulkOpts = new BulkOperationOptions
                {
                    AllowStale = true
                };

                var operation = documentStore.DatabaseCommands.DeleteByIndex(indexName, query, bulkOpts);

                // This is going to execute multiple "status check" requests to Raven, but
                // not much we can do about it
                operation.WaitForCompletion();
            }
            finally
            {
                doingCleanup = false;
            }
        }


    }
}