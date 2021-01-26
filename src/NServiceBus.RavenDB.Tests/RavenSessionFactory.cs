namespace NServiceBus.RavenDB.Tests
{
    using NServiceBus.Persistence.RavenDB;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;

    class RavenAsyncSessionFactory : IAsyncSessionProvider
    {
        IAsyncDocumentSession session;
        readonly IDocumentStore store;

        public RavenAsyncSessionFactory(IDocumentStore store)
        {
            session = null;
            this.store = store;
        }

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0074 // False positive
        public IAsyncDocumentSession AsyncSession => session ?? (session = OpenAsyncSession());
#pragma warning restore IDE0074 // False positive
#pragma warning restore IDE0079 // Remove unnecessary suppression


        IAsyncDocumentSession OpenAsyncSession()
        {
            var documentSession = store.OpenAsyncSession();
            documentSession.Advanced.UseOptimisticConcurrency = true;
            return documentSession;
        }

        public void ReleaseSession()
        {
            if (session == null)
            {
                return;
            }

            session.Dispose();
            session = null;
        }

        public void SaveChanges()
        {
            session?.SaveChangesAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
    }
}
