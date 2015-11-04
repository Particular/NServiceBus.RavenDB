namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Timeout.Core;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using CoreTimeoutData = NServiceBus.Timeout.Core.TimeoutData;
    using Timeout = TimeoutData;

    class TimeoutPersister : IPersistTimeouts
    {
        public TimeoutPersister(IDocumentStore store)
        {
            documentStore = store;
        }

        public async Task Add(CoreTimeoutData timeout, ContextBag context)
        {
            using (var session = documentStore.OpenAsyncSession())
            {
                await session.StoreAsync(new Timeout(timeout)).ConfigureAwait(false);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<bool> TryRemove(string timeoutId, ContextBag context)
        {
            using (var session = documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var timeout = await session.LoadAsync<Timeout>(timeoutId).ConfigureAwait(false);
                if (timeout == null)
                {
                     return false;
                }

                //deletes are performed on SaveChanges so this call is sync
                session.Delete(timeout);
               
                await session.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
        }

        public async Task<CoreTimeoutData> Peek(string timeoutId, ContextBag context)
        {
            using (var session = documentStore.OpenAsyncSession())
            {
                var timeoutData = await session.LoadAsync<Timeout>(timeoutId).ConfigureAwait(false);

                return  timeoutData?.ToCoreTimeoutData();
            }
        }

        public Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
        {
            return documentStore.AsyncDatabaseCommands.DeleteByIndexAsync("TimeoutsIndex", new IndexQuery
            {
                Query = $"SagaId:{sagaId}"
            }, new BulkOperationOptions
            {
                AllowStale = true
            });
        }

        readonly IDocumentStore documentStore;
    }
}