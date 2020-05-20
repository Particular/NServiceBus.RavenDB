namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations.CompareExchange;
    using Raven.Client.Documents.Session;

    class RavenDBSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public RavenDBSynchronizedStorageSession(IAsyncDocumentSession session, ContextBag context, bool callSaveChanges = true)
        {
            this.context = context;
            this.callSaveChanges = callSaveChanges;
            Session = session;

            // In order to make sure due to parent/child context inheritance for the holder to be retrievable we need to add it here
            this.context.Set(new SagaDataLeaseHolder());
        }

        public IAsyncDocumentSession Session { get; }

        public void Dispose()
        {
            // TODO: Think about the impact of call save changes, we potentially need to have the same logic in the outbox transaction then
            var holder = context.GetOrCreate<SagaDataLeaseHolder>();
            foreach (var nameAndIndex in holder.NamesAndIndex)
            {
                ReleaseResource(Session.Advanced.DocumentStore, nameAndIndex.Item1, nameAndIndex.Item2);
            }
        }

        // TODO: Maybe we could use the async APIs and make the dispose async void under the assumption releasing the lock is a best effort
        private void ReleaseResource(IDocumentStore store, string resourceName, long index)
        {
            var deleteResult = store.Operations.Send(new DeleteCompareExchangeValueOperation<SagaDataLease>(resourceName, index));

            if (deleteResult.Successful)
            {
                return;
            }

            // TODO: Meaningful exception
            throw new TimeoutException();

            // We have 2 options here:
            // deleteResult.Successful is true - we managed to release resource
            // deleteResult.Successful is false - someone else took the lock due to timeout
        }

        public Task CompleteAsync()
        {
            return callSaveChanges
                ? Session.SaveChangesAsync()
                : Task.CompletedTask;
        }

        readonly bool callSaveChanges;
        readonly ContextBag context;
    }
}