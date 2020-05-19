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
        }

        public IAsyncDocumentSession Session { get; }

        public void Dispose()
        {
            var holder = context.GetOrCreate<SagaDataLeaseHolder>();
            foreach (var nameAndIndex in holder.NamesAndIndex)
            {
                ReleaseResource(Session.Advanced.DocumentStore, nameAndIndex.Item1, nameAndIndex.Item2);
            }
        }

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