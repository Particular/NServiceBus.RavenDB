namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using NServiceBus.Persistence;
    using Raven.Client.Documents.Session;

    /// <summary>
    /// Synchronized storage session for wrapping RavenDB transactions
    /// </summary>
    class RavenDBSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        /// <summary>
        /// The RavenDB session
        /// </summary>
        public IAsyncDocumentSession Session { get; }

        /// <summary>
        /// Constructor for synchronized storage session
        /// </summary>
        /// <param name="session">The transaction to wrap</param>
        public RavenDBSynchronizedStorageSession(IAsyncDocumentSession session)
        {
            Session = session;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public Task CompleteAsync()
        {
            return Task.CompletedTask;
        }
    }
}