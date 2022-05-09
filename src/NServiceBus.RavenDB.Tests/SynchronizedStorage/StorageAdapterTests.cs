namespace NServiceBus.Persistence.RavenDB.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Tests;
    using NUnit.Framework;

    [TestFixture]
    public class StorageAdapterTests : RavenDBPersistenceTestBase
    {
        [Test]
        public async Task Should_use_existing_outbox_transaction()
        {
            var documentId = Guid.NewGuid().ToString("N");
            using (var outboxSession = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            using (var ravenDBOutboxTransaction = new RavenDBOutboxTransaction(outboxSession))
            using (var adaptedSession = new RavenDBSynchronizedStorageSession(CreateTestSessionOpener()))
            {
                await adaptedSession.TryOpen(ravenDBOutboxTransaction, new ContextBag());
                await adaptedSession.RavenSession().StoreAsync(new StorageAdapterTestDocument(), documentId);

                Assert.AreSame(outboxSession, adaptedSession.RavenSession());
                //Core commits both adapted and outbox sessions:
                await adaptedSession.CompleteAsync();
                await ravenDBOutboxTransaction.Commit();
            }

            using (var verificationSession = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                var document = await verificationSession.LoadAsync<StorageAdapterTestDocument>(documentId);
                Assert.IsNotNull(document);
                Assert.AreEqual(documentId, document.Id);
            }
        }

        [Test]
        public async Task Should_roll_back_with_existing_outbox_transaction()
        {
            var documentId = Guid.NewGuid().ToString("N");
            using (var outboxSession = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            using (var ravenDBOutboxTransaction = new RavenDBOutboxTransaction(outboxSession))
            using (var adaptedSession = new RavenDBSynchronizedStorageSession(CreateTestSessionOpener()))
            {
                await adaptedSession.TryOpen(ravenDBOutboxTransaction, new ContextBag());
                await adaptedSession.RavenSession().StoreAsync(new StorageAdapterTestDocument(), documentId);

                Assert.AreSame(outboxSession, adaptedSession.RavenSession());
                //await adaptedSession.CompleteAsync();
                //await ravenDBOutboxTransaction.Commit();
            }

            using (var verificationSession = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                var document = await verificationSession.LoadAsync<StorageAdapterTestDocument>(documentId);
                Assert.IsNull(document);
            }
        }

        [Test]
        public async Task Should_roll_back_with_existing_outbox_transaction_after_adapted_session_completed()
        {
            var documentId = Guid.NewGuid().ToString("N");
            using (var outboxSession = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            using (var ravenDBOutboxTransaction = new RavenDBOutboxTransaction(outboxSession))
            using (var adaptedSession = new RavenDBSynchronizedStorageSession(CreateTestSessionOpener()))
            {
                await adaptedSession.TryOpen(ravenDBOutboxTransaction, new ContextBag());
                await adaptedSession.RavenSession().StoreAsync(new StorageAdapterTestDocument(), documentId);

                Assert.AreSame(outboxSession, adaptedSession.RavenSession());
                // The adapted session can complete but a failure can happen at a later point to cause
                // the underlying outbox transaction to roll back
                await adaptedSession.CompleteAsync();
                //await ravenDBOutboxTransaction.Commit();
            }

            using (var verificationSession = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
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