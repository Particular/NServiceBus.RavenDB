namespace NServiceBus.RavenDB.Outbox
{
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using Raven.Client;

    class RavenDBOutboxTransaction : OutboxTransaction
    {
        public RavenDBOutboxTransaction(IDocumentSession session)
        {
            Session = session;
        }

        public void Dispose()
        {
            Session.Dispose();
            Session = null;
        }

        public Task Commit()
        {
            Session.SaveChanges();

            return Task.FromResult(0);
        }

        public IDocumentSession Session { get; private set; }
    }
}