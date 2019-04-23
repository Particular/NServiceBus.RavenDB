namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;

    class RavenDBSynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = contextBag.GetAsyncSession();
            var synchronizedStorageSession = new RavenDBSynchronizedStorageSession(session);
            return Task.FromResult((CompletableSynchronizedStorageSession)synchronizedStorageSession);
        }
    }
}