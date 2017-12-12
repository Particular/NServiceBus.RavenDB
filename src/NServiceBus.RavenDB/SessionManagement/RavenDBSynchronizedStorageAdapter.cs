namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Transport;

    class RavenDBSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult((CompletableSynchronizedStorageSession) null);

        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            var ravenTransaction = transaction as RavenDBOutboxTransaction;
            if (ravenTransaction != null)
            {
                CompletableSynchronizedStorageSession session = new RavenDBSynchronizedStorageSession(ravenTransaction.AsyncSession, false);
                return Task.FromResult(session);
            }
            return EmptyResult;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            // ReSharper disable once NotAccessedVariable - No way to just check for existence otherwise
            Transaction ambientTransaction;
            if (transportTransaction.TryGet(out ambientTransaction))
            {
                var session = context.GetAsyncSession();
                CompletableSynchronizedStorageSession completableSynchronizedStorageSession = new RavenDBSynchronizedStorageSession(session, true);
                return Task.FromResult(completableSynchronizedStorageSession);
            }
            return EmptyResult;
        }
    }
}