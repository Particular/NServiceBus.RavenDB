namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using Raven.Client.Documents.Session;

    class RavenDBSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public RavenDBSynchronizedStorageSession(IAsyncDocumentSession session, bool callSaveChanges = true)
        {
            this.callSaveChanges = callSaveChanges;
            Session = session;
        }

        public IAsyncDocumentSession Session { get; }

        public void Dispose()
        {
            if (callSaveChanges)
            {
                Session.Dispose();
            }
        }

        public Task CompleteAsync()
        {
            return callSaveChanges
                ? Session.SaveChangesAsync()
                : Task.CompletedTask;
        }

        readonly bool callSaveChanges;
    }
}