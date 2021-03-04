namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Transport;

    class RavenDBSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public RavenDBSynchronizedStorageAdapter(CurrentSessionHolder sessionHolder)
        {
            this.sessionHolder = sessionHolder;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            if (transaction is RavenDBOutboxTransaction outboxTransaction)
            {
                sessionHolder?.SetCurrentSession(outboxTransaction.AsyncSession);
                return Task.FromResult<CompletableSynchronizedStorageSession>(
                    new RavenDBSynchronizedStorageSession(outboxTransaction.AsyncSession, context, false));
            }

            return EmptyResult;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            // Since RavenDB doesn't support System.Transactions (or have transactions), there's no way to adapt anything out of the transport transaction.
            return EmptyResult;
        }

        static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult((CompletableSynchronizedStorageSession)null);
        readonly CurrentSessionHolder sessionHolder;
    }
}