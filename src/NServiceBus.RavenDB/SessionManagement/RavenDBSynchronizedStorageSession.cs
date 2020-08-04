namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using Raven.Client.Documents.Operations.CompareExchange;
    using Raven.Client.Documents.Session;

    class RavenDBSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        static ILog logger = LogManager.GetLogger("RavenLocking");

        public RavenDBSynchronizedStorageSession(IAsyncDocumentSession session, ContextBag context, bool callSaveChanges = true)
        {
            logger.Warn($"Start session {this.GetHashCode()}");
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
                logger.Warn($"Start releasing session {this.GetHashCode()} lock for {DocumentId}/{Index}");
                // We are optimistic and fire-and-forget the releasing of the lock and just continue. In case this fails the next message that needs to acquire the lock wil have to wait.
                _ = Session.Advanced.DocumentStore.Operations.SendAsync(new DeleteCompareExchangeValueOperation<SagaDataLease>(DocumentId, Index))
                    .ContinueWith(r =>
                    {
                        logger.Warn($"released lock for session {this.GetHashCode()} for document {DocumentId} with Index {Index}. Successful: {r.Result.Successful}.");
                    }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        public Task CompleteAsync()
        {
            logger.Warn($"Complete session {this.GetHashCode()}");
            return callSaveChanges
                ? Session.SaveChangesAsync()
                : Task.CompletedTask;
        }

        readonly bool callSaveChanges;
        readonly ContextBag context;
    }
}