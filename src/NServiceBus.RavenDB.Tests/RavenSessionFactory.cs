namespace NServiceBus.RavenDB.Tests
{
    using System.Threading.Tasks;
    using NServiceBus.RavenDB.Persistence;
    using Raven.Client;

    class RavenSessionFactory : ISessionProvider
    {
        IAsyncDocumentSession session;
        readonly IDocumentStore store;

        public RavenSessionFactory(IDocumentStore store)
        {
            session = null;
            this.store = store;
        }

        public IAsyncDocumentSession Session { get { return session ?? (session = OpenSession()); }}

        IAsyncDocumentSession OpenSession()
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

        public Task SaveChanges()
        {
            if (session == null)
                return Task.FromResult(0);

            return session.SaveChangesAsync();
        }
    }
}
