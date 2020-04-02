namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Transport;

    class RavenDBSynchronizedStorage : ISynchronizedStorage
    {
        IOpenRavenSessionsInPipeline sessionCreator;

        public RavenDBSynchronizedStorage(IOpenRavenSessionsInPipeline sessionCreator)
        {
            this.sessionCreator = sessionCreator;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag context)
        {
            var message = context.Get<IncomingMessage>();
            var session = sessionCreator.OpenSession(message.Headers);
            var synchronizedStorageSession = new RavenDBSynchronizedStorageSession(session);
            // for backwards compatibility
            //TODO check whether we can remove that. The adapter class would have to do the same
            //context.Set(session);
            return Task.FromResult((CompletableSynchronizedStorageSession)synchronizedStorageSession);
        }
    }
}