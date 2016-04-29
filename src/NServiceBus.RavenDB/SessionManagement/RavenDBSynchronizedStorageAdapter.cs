namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Transports;

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
            Transaction ambientTransaction;
            if (transportTransaction.TryGet(out ambientTransaction))
            {
                var ownership = context.GetSessionOwnership();
                CompletableSynchronizedStorageSession session = new RavenDBSynchronizedStorageSession(ownership.Session, ownership.Owns);
                return Task.FromResult(session);
            }
            return EmptyResult;
        }
    }
}