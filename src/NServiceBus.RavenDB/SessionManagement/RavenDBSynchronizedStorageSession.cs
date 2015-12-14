namespace NServiceBus.SagaPersisters.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Persistence;
    using Raven.Client;

    class RavenDBSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        bool ownsTransaction;

        public IAsyncDocumentSession Transaction { get; private set; }

        public RavenDBSynchronizedStorageSession(IAsyncDocumentSession session, bool ownsSession)
        {
            this.ownsTransaction = ownsSession;
            Transaction = session;
        }

        //TODO: Determine whether to keep this method based on the result of https://github.com/Particular/NServiceBus/pull/3170
        public async Task Enlist(Func<IAsyncDocumentSession, Task> action)
        {
            await action(Transaction);
        }

        public void Dispose()
        {
            if(ownsTransaction)
            {
                Transaction.Dispose();
            }
        }

        public async Task CompleteAsync()
        {
            if(ownsTransaction)
            {
                await Transaction.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}