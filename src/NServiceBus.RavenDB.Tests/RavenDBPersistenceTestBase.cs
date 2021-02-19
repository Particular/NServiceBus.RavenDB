namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Transport;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;

    public class RavenDBPersistenceTestBase
    {
        [SetUp]
        public virtual void SetUp()
        {
            db = new ReusableDB();
            var docStore = db.NewStore();
            CustomizeDocumentStore(docStore);
            docStore.Initialize();
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

        protected void WaitForIndexing()
        {
            db.WaitForIndexing(store);
        }

        protected static Task<Exception> Catch(Func<Task> action)
        {
            return Catch<Exception>(action);
        }

        /// <summary>
        ///     This helper is necessary because RavenTestBase doesn't like Assert.Throws, Assert.That... with async void methods.
        /// </summary>
        protected static async Task<TException> Catch<TException>(Func<Task> action) where TException : Exception
        {
            try
            {
                await action();
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

        protected IDocumentStore store;
        ReusableDB db;

        internal IOpenTenantAwareRavenSessions CreateTestSessionOpener(bool useClusterWideTransactions)
        {
            return new TestOpenSessionsInPipeline(store, useClusterWideTransactions);
        }

        class TestOpenSessionsInPipeline : IOpenTenantAwareRavenSessions
        {
            IDocumentStore store;
            bool useClusterWideTx;

            public TestOpenSessionsInPipeline(IDocumentStore store, bool useClusterWideTx)
            {
                this.store = store;
                this.useClusterWideTx = useClusterWideTx;
            }

            public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders)
            {
                var sessionOptions = new SessionOptions
                {
                    TransactionMode = useClusterWideTx ? TransactionMode.ClusterWide : TransactionMode.SingleNode
                };
                return store.OpenAsyncSession(sessionOptions).UsingOptimisticConcurrency(useClusterWideTx);
            }
        }
    }
}