namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    class RavenDBSynchronizedStorage : ISynchronizedStorage
    {
        public RavenDBSynchronizedStorage(IOpenTenantAwareRavenSessions sessionCreator, CurrentSessionHolder sessionHolder)
        {
            this.sessionCreator = sessionCreator;
            this.sessionHolder = sessionHolder;
        }

        public Task<ICompletableSynchronizedStorageSession> OpenSession(ContextBag context, CancellationToken cancellationToken = default)
        {
            var message = context.Get<IncomingMessage>();
            var session = sessionCreator.OpenSession(message.Headers);
            var synchronizedStorageSession = new RavenDBSynchronizedStorageSession(session, context, true);

            sessionHolder?.SetCurrentSession(session);

            return Task.FromResult((ICompletableSynchronizedStorageSession)synchronizedStorageSession);
        }

        IOpenTenantAwareRavenSessions sessionCreator;
        readonly CurrentSessionHolder sessionHolder;
    }
}