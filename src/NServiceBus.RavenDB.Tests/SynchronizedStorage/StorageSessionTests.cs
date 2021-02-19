namespace NServiceBus.Persistence.RavenDB.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Tests;
    using NUnit.Framework;
    using Raven.Client.Documents.Session;

    [TestFixture]
    public class StorageSessionTests : RavenDBPersistenceTestBase
    {
        class TestDocument
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task CompleteAsync_with_savechanges_enabled_completes_transaction(bool useClusterWideTx)
        {
            var newDocument = new TestDocument { Value = "42" };

            var sessionOptions = new SessionOptions()
            {
                TransactionMode = useClusterWideTx ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            };
            using (var writeDocSession = store.OpenAsyncSession(sessionOptions).UsingOptimisticConcurrency(useClusterWideTx))
            using (var writeSession = new RavenDBSynchronizedStorageSession(writeDocSession, new ContextBag(), true))
            {
                await writeSession.Session.StoreAsync(newDocument);
                await writeSession.CompleteAsync();
            }

            using (var readSession = store.OpenAsyncSession().UsingOptimisticConcurrency(false))
            {
                var storedDocument = await readSession.LoadAsync<TestDocument>(newDocument.Id);

                Assert.NotNull(storedDocument);
                Assert.AreEqual(newDocument.Value, storedDocument.Value);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Dispose_without_complete_rolls_back(bool useClusterWideTx)
        {
            var documentId = Guid.NewGuid().ToString();
            var sessionOptions = new SessionOptions()
            {
                TransactionMode = useClusterWideTx ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            };
            using (var writeDocSession = store.OpenAsyncSession(sessionOptions).UsingOptimisticConcurrency(false))
            using (var writeSession = new RavenDBSynchronizedStorageSession(writeDocSession, new ContextBag(), true))
            {
                await writeSession.Session.StoreAsync(new TestDocument { Value = "43" }, documentId);
                // do not call CompleteAsync
            }

            using (var readSession = store.OpenAsyncSession().UsingOptimisticConcurrency(false))
            {
                var storedDocument = await readSession.LoadAsync<TestDocument>(documentId);

                Assert.IsNull(storedDocument);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task CompleteAsync_without_savechanges_rolls_back(bool useClusterWideTx)
        {
            var documentId = Guid.NewGuid().ToString();
            var sessionOptions = new SessionOptions()
            {
                TransactionMode = useClusterWideTx ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            };
            using (var writeDocSession = store.OpenAsyncSession(sessionOptions).UsingOptimisticConcurrency(useClusterWideTx))
            using (var writeSession = new RavenDBSynchronizedStorageSession(writeDocSession, new ContextBag(), false))
            {
                await writeSession.Session.StoreAsync(new TestDocument { Value = "43" }, documentId);
                await writeSession.CompleteAsync();
            }

            using (var readSession = store.OpenAsyncSession().UsingOptimisticConcurrency(false))
            {
                var storedDocument = await readSession.LoadAsync<TestDocument>(documentId);

                Assert.IsNull(storedDocument);
            }
        }
    }
}