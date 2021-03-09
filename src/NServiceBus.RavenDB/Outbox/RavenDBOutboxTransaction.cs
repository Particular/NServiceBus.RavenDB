namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using Raven.Client.Documents.Session;

    class RavenDBOutboxTransaction : OutboxTransaction
    {
        public RavenDBOutboxTransaction(IAsyncDocumentSession session)
        {
            AsyncSession = session;
        }

        public IAsyncDocumentSession AsyncSession { get; private set; }

        public void Dispose()
        {
            AsyncSession?.Dispose();
            AsyncSession = null;
        }

        public Task Commit(CancellationToken cancellationToken = default)
        {
            return AsyncSession.SaveChangesAsync(cancellationToken);
        }
    }
}