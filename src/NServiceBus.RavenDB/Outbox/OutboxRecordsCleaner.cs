namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using Raven.Abstractions.Data;
    using Raven.Client;

    class OutboxRecordsCleaner
    {
        public OutboxRecordsCleaner(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public Task RemoveEntriesOlderThan(DateTime dateTime)
        {
            var query = new IndexQuery
            {
                Query = $"DispatchedAt:[* TO {dateTime:o}]"
            };

            var bulkOpts = new BulkOperationOptions
            {
                AllowStale = true
            };

            var operation = documentStore.DatabaseCommands.DeleteByIndex(nameof(OutboxRecordsIndex), query, bulkOpts);
            return operation.WaitForCompletionAsync();
        }

        IDocumentStore documentStore;
    }
}