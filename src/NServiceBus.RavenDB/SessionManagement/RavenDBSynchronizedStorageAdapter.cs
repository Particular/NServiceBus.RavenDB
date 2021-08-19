namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading;
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

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transaction is RavenDBOutboxTransaction IOutboxTransaction)
            {
                sessionHolder?.SetCurrentSession(IOutboxTransaction.AsyncSession);
                return Task.FromResult<ICompletableSynchronizedStorageSession>(
                    new RavenDBSynchronizedStorageSession(IOutboxTransaction.AsyncSession, context, false));
            }

            return EmptyResult;
        }

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            // Since RavenDB doesn't support System.Transactions (or have transactions), there's no way to adapt anything out of the transport transaction.
            return EmptyResult;
        }

        static readonly Task<ICompletableSynchronizedStorageSession> EmptyResult = Task.FromResult((ICompletableSynchronizedStorageSession)null);
        readonly CurrentSessionHolder sessionHolder;
    }
}