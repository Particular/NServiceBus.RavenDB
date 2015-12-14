namespace NServiceBus.RavenDB.Tests
{
    using NServiceBus.RavenDB.Persistence;
    using Raven.Client;

    class RavenAsyncSessionFactory : IAsyncSessionProvider
    {
        IAsyncDocumentSession session;
        readonly IDocumentStore store;

        public RavenAsyncSessionFactory(IDocumentStore store)
        {
            session = null;
            this.store = store;
        }

        public IAsyncDocumentSession AsyncSession => session ?? (session = OpenAsyncSession());

        IAsyncDocumentSession OpenAsyncSession()
        {
            var documentSession = store.OpenAsyncSession();
            documentSession.Advanced.AllowNonAuthoritativeInformation = false;
            documentSession.Advanced.UseOptimisticConcurrency = true;
            return documentSession;
        }

        public void ReleaseSession()
        {
            if (session == null)
                return;

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
