namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Timeout.Core;
    using Raven.Abstractions.Data;
    using Raven.Abstractions.Exceptions;
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
            using (var session = documentStore.OpenSession())
            {
                var timeoutData = new Timeout(timeout);
                await session.StoreAsync(timeoutData).ConfigureAwait(false);
                await session.SaveChangesAsync().ConfigureAwait(false);
                timeout.Id = timeoutData.Id;
            }
        }

        public async Task<bool> TryRemove(string timeoutId, ContextBag context)
        {
            using (var session = documentStore.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

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
            using (var session = documentStore.OpenSession())
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