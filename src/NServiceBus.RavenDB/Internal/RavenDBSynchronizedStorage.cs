namespace NServiceBus.RavenDB.Internal
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.RavenDB.SessionManagement;
    using NServiceBus.SagaPersisters.RavenDB;

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