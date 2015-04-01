namespace NServiceBus.RavenDB.Tests
{
    using NServiceBus.RavenDB.Persistence;
    using Raven.Client;

    class RavenSessionFactory : ISessionProvider
    {
        IDocumentSession session;
        readonly IDocumentStore store;

        public RavenSessionFactory(IDocumentStore store)
        {
            session = null;
            this.store = store;
        }

        public IDocumentSession Session => session ?? (session = OpenSession());

        IDocumentSession OpenSession()
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
            session?.SaveChanges();
        }
    }
}
