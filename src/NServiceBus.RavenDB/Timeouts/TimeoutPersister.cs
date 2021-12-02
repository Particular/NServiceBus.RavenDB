namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Timeout.Core;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations;
    using Raven.Client.Documents.Queries;
    using Raven.Client.Documents.Session;
    using Raven.Client.Exceptions;
    using CoreTimeoutData = Timeout.Core.TimeoutData;
    using Timeout = TimeoutPersisters.RavenDB.TimeoutData;

    class TimeoutPersister : IPersistTimeouts
    {
        public TimeoutPersister(IDocumentStore store, bool useClusterWideTransactions)
        {
            this.useClusterWideTransactions = useClusterWideTransactions;
            documentStore = store;
        }

        public async Task Add(CoreTimeoutData timeout, ContextBag context)
        {
            using (var session = OpenAsyncSession())
            {
                var timeoutData = new Timeout(timeout);
                await session.StoreAsync(timeoutData).ConfigureAwait(false);
                session.StoreSchemaVersionInMetadata(timeoutData);
                await session.SaveChangesAsync().ConfigureAwait(false);
                timeout.Id = timeoutData.Id;
            }
        }

        public async Task<bool> TryRemove(string timeoutId, ContextBag context)
        {
            using (var session = OpenAsyncSession())
            {
                if (!useClusterWideTransactions)
                {
                    session.Advanced.UseOptimisticConcurrency = true;
                }

                var timeout = await session.LoadAsync<Timeout>(timeoutId).ConfigureAwait(false);
                if (timeout == null)
                {
                    return false;
                }

                //deletes are performed on SaveChanges so this call is sync
                session.Delete(timeout);

                try
                {
                    await session.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (ConcurrencyException)
                {
                    return false;
                }

                return true;
            }
        }

        public async Task<CoreTimeoutData> Peek(string timeoutId, ContextBag context)
        {
            using (var session = OpenAsyncSession())
            {
                var timeoutData = await session.LoadAsync<Timeout>(timeoutId).ConfigureAwait(false);

                return timeoutData?.ToCoreTimeoutData();
            }
        }

        IAsyncDocumentSession OpenAsyncSession() =>
            documentStore.OpenAsyncSession(new SessionOptions
            {
                TransactionMode = useClusterWideTransactions ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            });

        public Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
        {
            var options = new QueryOperationOptions { AllowStale = true };
            var deleteOp = new DeleteByQueryOperation<Timeout, TimeoutsIndex>(timeout => timeout.SagaId == sagaId, options);

            return documentStore.Operations.SendAsync(deleteOp);
        }

        readonly IDocumentStore documentStore;
        readonly bool useClusterWideTransactions;
    }
}