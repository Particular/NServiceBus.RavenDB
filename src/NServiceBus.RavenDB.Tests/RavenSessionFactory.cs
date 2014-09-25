namespace NServiceBus.RavenDB.Tests
{
    using Persistence;
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

        public IDocumentSession Session { get { return session ?? (session = OpenSession()); }}

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
            if (session == null)
                return;

            session.SaveChanges();
        }
    }
}
