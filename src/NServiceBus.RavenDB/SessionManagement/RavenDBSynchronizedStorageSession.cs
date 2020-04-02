namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using NServiceBus.Persistence;
    using Raven.Client.Documents.Session;

    class RavenDBSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public IAsyncDocumentSession Session { get; }

        public RavenDBSynchronizedStorageSession(IAsyncDocumentSession session)
        {
            Session = session;
        }

        public void Dispose()
        {
        }

        public Task CompleteAsync()
        {
            return Session.SaveChangesAsync();
        }
    }
}