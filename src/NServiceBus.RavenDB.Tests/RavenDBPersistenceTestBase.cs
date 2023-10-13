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
        SessionOptions sessionOptions;

        [SetUp]
        public virtual async Task SetUp()
        {
            db = new ReusableDB();
            IDocumentStore docStore = db.NewStore();
            CustomizeDocumentStore(docStore);
            docStore.Initialize();
            await db.EnsureDatabaseExists(docStore);
            store = docStore;
            sessionOptions = new SessionOptions
            {
                TransactionMode = UseClusterWideTransactions ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            };
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

        protected bool UseClusterWideTransactions => db.UseClusterWideTransactions;

        protected SessionOptions GetSessionOptions()
        {
            return sessionOptions;
        }

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
            messageId ??= Guid.NewGuid().ToString("N");

            var incomingMessage = new IncomingMessage(messageId, [], new byte[0]);

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