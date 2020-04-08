namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Transport;

    class RavenDBSynchronizedStorage : ISynchronizedStorage
    {
        IOpenTenantAwareRavenSessions sessionCreator;

        public RavenDBSynchronizedStorage(IOpenTenantAwareRavenSessions sessionCreator)
        {
            this.sessionCreator = sessionCreator;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag context)
        {
            var message = context.Get<IncomingMessage>();
            var session = sessionCreator.OpenSession(message.Headers);
            var synchronizedStorageSession = new RavenDBSynchronizedStorageSession(session, true);

            return Task.FromResult((CompletableSynchronizedStorageSession)synchronizedStorageSession);
        }
    }
}