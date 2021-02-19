namespace NServiceBus.Persistence.RavenDB.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Tests;
    using NUnit.Framework;
    using Raven.Client.Documents.Session;

    [TestFixture]
    public class StorageAdapterTests : RavenDBPersistenceTestBase
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_use_existing_outbox_transaction(bool useClusterWideTx)
        {
            var documentId = Guid.NewGuid().ToString("N");

            var sessionOptions = new SessionOptions()
            {
                TransactionMode = useClusterWideTx ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            };
            var storageAdapter = new RavenDBSynchronizedStorageAdapter();
            using (var outboxSession = store.OpenAsyncSession(sessionOptions).UsingOptimisticConcurrency(useClusterWideTx))
            using (var ravenDBOutboxTransaction = new RavenDBOutboxTransaction(outboxSession))
            using (var adaptedSession = await storageAdapter.TryAdapt(ravenDBOutboxTransaction, new ContextBag()))
            {
                await adaptedSession.RavenSession().StoreAsync(new StorageAdapterTestDocument(), documentId);

                Assert.AreSame(outboxSession, adaptedSession.RavenSession());
                //Core commits both adapted and outbox sessions:
                await adaptedSession.CompleteAsync();
                await ravenDBOutboxTransaction.Commit();
            }

            using (var verificationSession = store.OpenAsyncSession().UsingOptimisticConcurrency(false))
            {
                var document = await verificationSession.LoadAsync<StorageAdapterTestDocument>(documentId);
                Assert.IsNotNull(document);
                Assert.AreEqual(documentId, document.Id);
            }
        }


        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_roll_back_with_existing_outbox_transaction(bool useClusterWideTx)
        {
            var documentId = Guid.NewGuid().ToString("N");

            var sessionOptions = new SessionOptions()
            {
                TransactionMode = useClusterWideTx ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            };
            var storageAdapter = new RavenDBSynchronizedStorageAdapter();
            using (var outboxSession = store.OpenAsyncSession(sessionOptions).UsingOptimisticConcurrency(useClusterWideTx))
            using (var ravenDBOutboxTransaction = new RavenDBOutboxTransaction(outboxSession))
            using (var adaptedSession = await storageAdapter.TryAdapt(ravenDBOutboxTransaction, new ContextBag()))
            {
                await adaptedSession.RavenSession().StoreAsync(new StorageAdapterTestDocument(), documentId);

                Assert.AreSame(outboxSession, adaptedSession.RavenSession());
                //await adaptedSession.CompleteAsync();
                //await ravenDBOutboxTransaction.Commit();
            }

            using (var verificationSession = store.OpenAsyncSession().UsingOptimisticConcurrency(false))
            {
                var document = await verificationSession.LoadAsync<StorageAdapterTestDocument>(documentId);
                Assert.IsNull(document);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_roll_back_with_existing_outbox_transaction_after_adapted_session_completed(bool useClusterWideTx)
        {
            var documentId = Guid.NewGuid().ToString("N");

            var sessionOptions = new SessionOptions()
            {
                TransactionMode = useClusterWideTx ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            };
            var storageAdapter = new RavenDBSynchronizedStorageAdapter();
            using (var outboxSession = store.OpenAsyncSession(sessionOptions).UsingOptimisticConcurrency(useClusterWideTx))
            using (var ravenDBOutboxTransaction = new RavenDBOutboxTransaction(outboxSession))
            using (var adaptedSession = await storageAdapter.TryAdapt(ravenDBOutboxTransaction, new ContextBag()))
            {
                await adaptedSession.RavenSession().StoreAsync(new StorageAdapterTestDocument(), documentId);

                Assert.AreSame(outboxSession, adaptedSession.RavenSession());
                // The adapted session can complete but a failure can happen at a later point to cause
                // the underlying outbox transaction to roll back
                await adaptedSession.CompleteAsync();
                //await ravenDBOutboxTransaction.Commit();
            }

            using (var verificationSession = store.OpenAsyncSession().UsingOptimisticConcurrency(false))
            {
                var document = await verificationSession.LoadAsync<StorageAdapterTestDocument>(documentId);
                Assert.IsNull(document);
            }
        }

        class StorageAdapterTestDocument
        {
            public string Id { get; set; }
        }
    }
}