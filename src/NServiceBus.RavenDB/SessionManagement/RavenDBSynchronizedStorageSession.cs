namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Raven.Client.Documents.Operations.CompareExchange;
    using Raven.Client.Documents.Session;

    class RavenDBSynchronizedStorageSession : ICompletableSynchronizedStorageSession
    {
        public RavenDBSynchronizedStorageSession(IAsyncDocumentSession session, ContextBag context, bool callSaveChanges = true)
        {
            this.callSaveChanges = callSaveChanges;
            this.context = context;
            Session = session;

            // In order to make sure due to parent/child context inheritance for the holder to be retrievable we need to add it here
            this.context.Set(new SagaDataLeaseHolder());
        }

        public IAsyncDocumentSession Session { get; }

        public void Dispose()
        {
            // Releasing locks here at the latest point possible to prevent issues with other pipeline resources depending on the lock.
            var holder = context.Get<SagaDataLeaseHolder>();
            foreach (var (DocumentId, Index) in holder.DocumentsIdsAndIndexes)
            {
                // We are optimistic and fire-and-forget the releasing of the lock and just continue. In case this fails the next message that needs to acquire the lock wil have to wait.
                _ = Session.Advanced.DocumentStore.Operations.SendAsync(new DeleteCompareExchangeValueOperation<SagaDataLease>(DocumentId, Index));
            }
        }

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            return callSaveChanges
                ? Session.SaveChangesAsync(cancellationToken)
                : Task.CompletedTask;
        }

        readonly bool callSaveChanges;
        readonly ContextBag context;
    }
}