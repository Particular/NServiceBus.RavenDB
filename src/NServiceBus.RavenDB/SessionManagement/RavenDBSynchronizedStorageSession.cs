namespace NServiceBus.SagaPersisters.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Persistence;
    using Raven.Client;

    /// <summary>
    /// Synchronized storage session for wrapping RavenDB transactions
    /// </summary>
    public class RavenDBSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        private bool ownsTransaction;

        /// <summary>
        /// The RavenDB session
        /// </summary>
        public IAsyncDocumentSession Transaction { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session">The transaction to wrap</param>
        /// <param name="ownsSession">Whether this instance is responsible for committing and disposing</param>
        public RavenDBSynchronizedStorageSession(IAsyncDocumentSession session, bool ownsSession)
        {
            this.ownsTransaction = ownsSession;
            Transaction = session;
        }

        /// <summary>
        /// Enlist and run an action against the RavenDB session
        /// </summary>
        public async Task Enlist(Func<IAsyncDocumentSession, Task> action)
        {
            await action(Transaction);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if(ownsTransaction)
            {
                Transaction.Dispose();
            }
        }

        /// <inheritdoc />
        public async Task CompleteAsync()
        {
            if(ownsTransaction)
            {
                await Transaction.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}