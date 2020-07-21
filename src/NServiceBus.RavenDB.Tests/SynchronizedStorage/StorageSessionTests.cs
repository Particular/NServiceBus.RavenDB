﻿namespace NServiceBus.Persistence.RavenDB.Tests
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
            using (var writeDocSession = store.OpenAsyncSession().UsingOptimisticConcurrency())
            using (var writeSession = new RavenDBSynchronizedStorageSession(writeDocSession, new ContextBag(), true))
            {
                await writeSession.Session.StoreAsync(newDocument);
                await writeSession.CompleteAsync();
            }

            using (var readSession = store.OpenAsyncSession().UsingOptimisticConcurrency())
            {
                var storedDocument = await readSession.LoadAsync<TestDocument>(newDocument.Id);

                Assert.NotNull(storedDocument);
                Assert.AreEqual(newDocument.Value, storedDocument.Value);
            }
        }

        [Test]
        public async Task Dispose_without_complete_rolls_back()
        {
            var documentId = Guid.NewGuid().ToString();
            using (var writeDocSession = store.OpenAsyncSession().UsingOptimisticConcurrency())
            using (var writeSession = new RavenDBSynchronizedStorageSession(writeDocSession, new ContextBag(), true))
            {
                await writeSession.Session.StoreAsync(new TestDocument { Value = "43" }, documentId);
                // do not call CompleteAsync
            }

            using (var readSession = store.OpenAsyncSession().UsingOptimisticConcurrency())
            {
                var storedDocument = await readSession.LoadAsync<TestDocument>(documentId);

                Assert.IsNull(storedDocument);
            }
        }

        [Test]
        public async Task CompleteAsync_without_savechanges_rolls_back()
        {
            var documentId = Guid.NewGuid().ToString();
            using (var writeDocSession = store.OpenAsyncSession().UsingOptimisticConcurrency())
            using (var writeSession = new RavenDBSynchronizedStorageSession(writeDocSession, new ContextBag(), false))
            {
                await writeSession.Session.StoreAsync(new TestDocument { Value = "43" }, documentId);
                await writeSession.CompleteAsync();
            }

            using (var readSession = store.OpenAsyncSession().UsingOptimisticConcurrency())
            {
                var storedDocument = await readSession.LoadAsync<TestDocument>(documentId);

                Assert.IsNull(storedDocument);
            }
        }
    }
}