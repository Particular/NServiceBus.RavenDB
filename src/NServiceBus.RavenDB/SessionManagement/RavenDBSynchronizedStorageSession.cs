namespace NServiceBus.Persistence.RavenDB;

using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Outbox;
using Raven.Client.Documents.Operations.CompareExchange;
using Raven.Client.Documents.Session;
using Transport;

sealed class RavenDBSynchronizedStorageSession : ICompletableSynchronizedStorageSession
{
    public RavenDBSynchronizedStorageSession(IOpenTenantAwareRavenSessions sessionCreator)
        => this.sessionCreator = sessionCreator;

    public IAsyncDocumentSession Session { get; private set; }

    public void Dispose()
    {
        if (context == null || disposed)
        {
            return;
        }

        // Releasing locks here at the latest point possible to prevent issues with other pipeline resources depending on the lock.
        var holder = context.Get<SagaDataLeaseHolder>();
        foreach (var (documentId, index) in holder.DocumentsIdsAndIndexes)
        {
            // We are optimistic and fire-and-forget the releasing of the lock and just continue. In case this fails the next message that needs to acquire the lock wil have to wait.
            _ = Session.Advanced.DocumentStore.Operations.SendAsync(new DeleteCompareExchangeValueOperation<SagaDataLease>(documentId, index));
        }

        disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();

        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag contextBag, CancellationToken cancellationToken = default)
    {
        if (transaction is RavenDBOutboxTransaction outboxTransaction)
        {
            callSaveChanges = false;
            context = contextBag;
            context.Set(new SagaDataLeaseHolder());
            Session = outboxTransaction.AsyncSession;
            return new ValueTask<bool>(true);
        }

        return new ValueTask<bool>(false);
    }

    public ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag contextBag,
        CancellationToken cancellationToken = default) =>
        // Since RavenDB doesn't support System.Transactions (or have transactions), there's no way to adapt anything out of the transport transaction.
        new(false);

    public Task Open(ContextBag contextBag, CancellationToken cancellationToken = default)
    {
        var message = contextBag.Get<IncomingMessage>();
        Session = sessionCreator.OpenSession(message.Headers);

        callSaveChanges = true;
        context = contextBag;
        context.Set(new SagaDataLeaseHolder());

        return Task.CompletedTask;
    }

    public Task CompleteAsync(CancellationToken cancellationToken = default) =>
        callSaveChanges
            ? Session.SaveChangesAsync(cancellationToken)
            : Task.CompletedTask;

    readonly IOpenTenantAwareRavenSessions sessionCreator;
    bool callSaveChanges;
    ContextBag context;
    bool disposed;
}