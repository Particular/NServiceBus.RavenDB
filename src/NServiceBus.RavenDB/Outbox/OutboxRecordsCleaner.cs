namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Raven.Abstractions.Data;
    using Raven.Client;

    class OutboxRecordsCleaner
    {
        public OutboxRecordsCleaner(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
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

            var operation = await documentStore.AsyncDatabaseCommands.DeleteByIndexAsync("OutboxRecordsIndex", query, bulkOpts, cancellationToken)
                .ConfigureAwait(false);

            // This is going to execute multiple "status check" requests to Raven, but this does
            // not currently support CancellationToken.
            await operation.WaitForCompletionAsync().ConfigureAwait(false);
        }

        IDocumentStore documentStore;
    }
}