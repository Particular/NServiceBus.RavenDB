namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Raven.Client.Documents.Session;
    using Transport;

    class RavenDBSynchronizedStorage : ISynchronizedStorage
    {
        public RavenDBSynchronizedStorage(IOpenTenantAwareRavenSessions sessionCreator, CurrentSessionHolder sessionHolder, bool useClusterWideTransactions)
        {
            this.sessionCreator = sessionCreator;
            this.sessionHolder = sessionHolder;
            this.useClusterWideTransactions = useClusterWideTransactions;
        }

        public Task<ICompletableSynchronizedStorageSession> OpenSession(ContextBag context, CancellationToken cancellationToken = default)
        {
            var message = context.Get<IncomingMessage>();
            var session = sessionCreator.OpenSession(message.Headers, new SessionOptions
            {
                TransactionMode = useClusterWideTransactions ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            });
            var synchronizedStorageSession = new RavenDBSynchronizedStorageSession(session, context, true);

            sessionHolder?.SetCurrentSession(session);

            return Task.FromResult((ICompletableSynchronizedStorageSession)synchronizedStorageSession);
        }

        IOpenTenantAwareRavenSessions sessionCreator;
        readonly CurrentSessionHolder sessionHolder;
        readonly bool useClusterWideTransactions;
    }
}