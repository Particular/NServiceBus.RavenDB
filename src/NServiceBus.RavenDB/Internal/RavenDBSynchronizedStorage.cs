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
            var synchronizedStorageSession = new RavenDBSynchronizedStorageSession(session, false);
            return Task.FromResult((CompletableSynchronizedStorageSession) synchronizedStorageSession);
        }
    }
}