namespace NServiceBus.RavenDB.Outbox
{
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using Raven.Client;

    class RavenDBOutboxTransaction : OutboxTransaction
    {
        public RavenDBOutboxTransaction(IAsyncDocumentSession session)
        {
            AsyncSession = session;
        }

        public void Dispose()
        {
            AsyncSession.Dispose();
            AsyncSession = null;
        }

        public async Task Commit()
        {
            await AsyncSession.SaveChangesAsync().ConfigureAwait(false);
        }

        public IAsyncDocumentSession AsyncSession { get; private set; }
    }
}