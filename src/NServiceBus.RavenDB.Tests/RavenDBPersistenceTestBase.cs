namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;
    using Transport;

    public abstract class RavenDBPersistenceTestBase
    {
        IReusableDB db;

        protected IDocumentStore store;

        [SetUp]
        public virtual async Task SetUp()
        {
            db = new ReusableDB();
            IDocumentStore docStore = db.NewStore();
            CustomizeDocumentStore(docStore);
            docStore.Initialize();
            await db.EnsureDatabaseExists(docStore);
            store = docStore;
        }

        protected virtual void CustomizeDocumentStore(IDocumentStore docStore)
        {
        }

        [TearDown]
        public virtual void TearDown()
        {
            store.Dispose();
            db.Dispose();
        }

        protected Task WaitForIndexing(CancellationToken cancellationToken = default) =>
            db.WaitForIndexing(store, cancellationToken);

        protected bool UseClusterWideTransactions => db.GetTransactionMode;

        /// <summary>
        ///     This helper is necessary because RavenTestBase doesn't like Assert.Throws, Assert.That... with async void methods.
        /// </summary>
        protected static async Task<TException> Catch<TException>(Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default) where TException : Exception
        {
            try
            {
                await action(cancellationToken);
                return default;
            }
            catch (TException ex)
            {
                return ex;
            }
        }

        protected IncomingMessage SimulateIncomingMessage(ContextBag context, string messageId = null)
        {
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0054 // False positive
            messageId = messageId ?? Guid.NewGuid().ToString("N");
#pragma warning restore IDE0054 // False positive
#pragma warning restore IDE0079 // Remove unnecessary suppression

            var incomingMessage = new IncomingMessage(messageId, new Dictionary<string, string>(), new byte[0]);

            context.Set(incomingMessage);

            return incomingMessage;
        }

        internal IOpenTenantAwareRavenSessions CreateTestSessionOpener() => new TestOpenSessionsInPipeline(store, UseClusterWideTransactions);

        class TestOpenSessionsInPipeline : IOpenTenantAwareRavenSessions
        {
            readonly bool useClusterWideTx;
            readonly IDocumentStore store;

            public TestOpenSessionsInPipeline(IDocumentStore store, bool useClusterWideTx)
            {
                this.store = store;
                this.useClusterWideTx = useClusterWideTx;
            }

            public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders) => store.OpenAsyncSession(new SessionOptions
            {
                TransactionMode = useClusterWideTx ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            });
        }
    }
}