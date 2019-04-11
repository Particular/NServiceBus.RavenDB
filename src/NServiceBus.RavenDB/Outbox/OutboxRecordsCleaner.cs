namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Queries;

    class OutboxRecordsCleaner
    {
        public OutboxRecordsCleaner(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
            indexName = new OutboxRecordsIndex().IndexName;
        }

        public async Task RemoveEntriesOlderThan(DateTime dateTime, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = new IndexQuery
            {
                Query = $"Dispatched:true AND DispatchedAt:[* TO {dateTime:o}]"
            };

            var bulkOpts = new BulkOperationOptions
            {
                AllowStale = true
            };

            var operation = await documentStore.AsyncDatabaseCommands.DeleteByIndexAsync(indexName, query, bulkOpts, cancellationToken)
                .ConfigureAwait(false);

            // This is going to execute multiple "status check" requests to Raven, but this does
            // not currently support CancellationToken.
            await operation.WaitForCompletionAsync().ConfigureAwait(false);
        }

        IDocumentStore documentStore;
        string indexName;
    }
}