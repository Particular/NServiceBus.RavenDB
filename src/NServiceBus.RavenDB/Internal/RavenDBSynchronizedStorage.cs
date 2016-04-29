namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;

    class RavenDBSynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var ownership = contextBag.GetSessionOwnership();
            var synchronizedStorageSession = new RavenDBSynchronizedStorageSession(ownership.Session, ownership.Owns);
            return Task.FromResult((CompletableSynchronizedStorageSession) synchronizedStorageSession);
        }
    }
}