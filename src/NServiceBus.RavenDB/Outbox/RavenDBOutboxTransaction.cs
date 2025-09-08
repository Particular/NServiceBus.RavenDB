namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using Raven.Client.Documents.Session;

    sealed class RavenDBOutboxTransaction(IAsyncDocumentSession session) : IOutboxTransaction
    {
        public IAsyncDocumentSession AsyncSession { get; private set; } = session;

        public void Dispose()
        {
            AsyncSession?.Dispose();
            AsyncSession = null;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        public Task Commit(CancellationToken cancellationToken = default)
            => AsyncSession.SaveChangesAsync(cancellationToken);
    }
}