namespace NServiceBus.Persistence.RavenDB.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Tests;
    using NUnit.Framework;

    [TestFixture]
    public class StorageSessionTests : RavenDBPersistenceTestBase
    {
        class TestDocument
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }

        [Test]
        public async Task CompleteAsync_with_savechanges_enabled_completes_transaction()
        {
            var newDocument = new TestDocument { Value = "42" };
            using (var writeSession = new RavenDBSynchronizedStorageSession(CreateTestSessionOpener()))
            {
                var contextBag = new ContextBag();
                SimulateIncomingMessage(contextBag);
                await writeSession.Open(contextBag); //Owns the session so CompleteAsync commits the transaction
                await writeSession.Session.StoreAsync(newDocument);
                await writeSession.CompleteAsync();
            }

            using (var readSession = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                var storedDocument = await readSession.LoadAsync<TestDocument>(newDocument.Id);

                Assert.NotNull(storedDocument);
                Assert.That(storedDocument.Value, Is.EqualTo(newDocument.Value));
            }
        }

        [Test]
        public async Task Dispose_without_complete_rolls_back()
        {
            var documentId = Guid.NewGuid().ToString();
            using (var writeSession = new RavenDBSynchronizedStorageSession(CreateTestSessionOpener()))
            {
                var contextBag = new ContextBag();
                SimulateIncomingMessage(contextBag);
                await writeSession.Open(contextBag);
                await writeSession.Session.StoreAsync(new TestDocument { Value = "43" }, documentId);
                // do not call CompleteAsync.
            }

            using (var readSession = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                var storedDocument = await readSession.LoadAsync<TestDocument>(documentId);

                Assert.IsNull(storedDocument);
            }
        }

        [Test]
        public async Task CompleteAsync_without_savechanges_rolls_back()
        {
            var documentId = Guid.NewGuid().ToString();
            using (var writeDocSession = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            using (var writeSession = new RavenDBSynchronizedStorageSession(CreateTestSessionOpener()))
            {
                var contextBag = new ContextBag();
                SimulateIncomingMessage(contextBag);
                await writeSession.TryOpen(new RavenDBOutboxTransaction(writeDocSession), contextBag); //Does not own the RavenDB session so CompleteAsync is NOOP
                await writeSession.Session.StoreAsync(new TestDocument { Value = "43" }, documentId);
                await writeSession.CompleteAsync();
            }

            using (var readSession = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                var storedDocument = await readSession.LoadAsync<TestDocument>(documentId);

                Assert.IsNull(storedDocument);
            }
        }

        [Test]
        public async Task Dispose_multiple_times_works()
        {
            var newDocument = new TestDocument { Value = "42" };
            using (var writeSession = new RavenDBSynchronizedStorageSession(CreateTestSessionOpener()))
            {
                var contextBag = new ContextBag();
                SimulateIncomingMessage(contextBag);
                await writeSession.Open(contextBag); //Owns the session so CompleteAsync commits the transaction
                await writeSession.Session.StoreAsync(newDocument);
                await writeSession.CompleteAsync();

                // disposing multiple times
                writeSession.Dispose();
            }

            using (var readSession = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                var storedDocument = await readSession.LoadAsync<TestDocument>(newDocument.Id);

                Assert.NotNull(storedDocument);
                Assert.That(storedDocument.Value, Is.EqualTo(newDocument.Value));
            }
        }
    }
}