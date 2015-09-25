namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Timeout.Core;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using CoreTimeoutData = Timeout.Core.TimeoutData;
    using Timeout = TimeoutData;

    class TimeoutPersister : IPersistTimeouts
    {
        readonly IDocumentStore documentStore;

        public TimeoutPersister(IDocumentStore store)
        {
            documentStore = store;
        }

        public async Task Add(CoreTimeoutData timeout, TimeoutPersistenceOptions options)
        {
            using (var session = documentStore.OpenAsyncSession())
            {
                await session.StoreAsync(new Timeout(timeout)).ConfigureAwait(false);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<CoreTimeoutData> Remove(string timeoutId, TimeoutPersistenceOptions options)
        {
            using (var session = documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var timeout = await session.LoadAsync<Timeout>(timeoutId).ConfigureAwait(false);
                if (timeout == null)
                {
                    return null;
                }

                var timeoutData = timeout.ToCoreTimeoutData();
                session.Delete(timeout);
                await session.SaveChangesAsync().ConfigureAwait(false);
                return timeoutData;
            }
        }

        public Task RemoveTimeoutBy(Guid sagaId, TimeoutPersistenceOptions options)
        {
            return documentStore.AsyncDatabaseCommands.DeleteByIndexAsync("TimeoutsIndex", new IndexQuery { Query = $"SagaId:{sagaId}" }, new BulkOperationOptions { AllowStale = true });
        }
    }
}