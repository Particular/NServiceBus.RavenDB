namespace NServiceBus.RavenDB.Tests
{
    using NServiceBus.RavenDB.Persistence;
    using Raven.Client;

    class RavenAsyncSessionFactory : IAsyncSessionProvider
    {
        IDocumentSession session;
        readonly IDocumentStore store;

        public RavenAsyncSessionFactory(IDocumentStore store)
        {
            session = null;
            this.store = store;
        }

        public IDocumentSession AsyncSession => session ?? (session = OpenAsyncSession());

        IDocumentSession OpenAsyncSession()
        {
            var documentSession = store.OpenSession();
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
