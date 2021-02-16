namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.RavenDB.Outbox;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations;
    using Raven.Client.Documents.Queries;
    using Raven.Client.Documents.Session;

    class OutboxRecordsCleaner
    {
        public OutboxRecordsCleaner(IDocumentStore documentStore, bool useClusterWideTransactions)
        {
            this.documentStore = documentStore;
            this.useClusterWideTransactions = useClusterWideTransactions;
        }

        public async Task RemoveEntriesOlderThan(DateTime dateTime, CancellationToken cancellationToken = default)
        {
            var options = new QueryOperationOptions { AllowStale = true, RetrieveDetails = useClusterWideTransactions };
            var deleteOp = new DeleteByQueryOperation<OutboxRecord, OutboxRecordsIndex>(record => record.Dispatched && record.DispatchedAt <= dateTime, options);

            var operation = await documentStore.Operations.SendAsync(deleteOp, token: cancellationToken).ConfigureAwait(false);

            // This is going to execute multiple "status check" requests to Raven, but this does
            // not currently support CancellationToken.
            if (!useClusterWideTransactions)
            {
                await operation.WaitForCompletionAsync().ConfigureAwait(false);
            }
            else
            {
                var result = operation.WaitForCompletion<BulkOperationResult>();
                var compareExchangeKeysForDeletedRecords = result.Details.Select(x => $"{OutboxPersister.OutboxPersisterCompareExchangePrefix}/{((BulkOperationResult.DeleteDetails)x).Id}");

                var sessionOptions = new SessionOptions { TransactionMode = TransactionMode.ClusterWide };
                using (var session = documentStore.OpenAsyncSession(sessionOptions))
                {
                    foreach (var deletedCompareExchangeValueKey in compareExchangeKeysForDeletedRecords)
                    {
                        var cev = await session.Advanced.ClusterTransaction.GetCompareExchangeValueAsync<string>(
                            deletedCompareExchangeValueKey).ConfigureAwait(false);
                        session.Advanced.ClusterTransaction.DeleteCompareExchangeValue(cev);
                    }
                }
            }
        }

        IDocumentStore documentStore;
        bool useClusterWideTransactions;
    }
}