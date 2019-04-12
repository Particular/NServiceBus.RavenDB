namespace NServiceBus.RavenDB.Tests.Persistence.DocumentIds
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Queries;

    [TestFixture]
    public class InconsistentSagaIdConventions : DocumentIdConventionTestBase
    {
        [TestCase(ConventionType.RavenDefault)]
        [TestCase(ConventionType.NSBDefault)]
        [TestCase(ConventionType.Customer)]
        public async Task TestRetrievingSagas(ConventionType seedType)
        {
            using (var db = new ReusableDB())
            {
                List<TestSagaData> sagas;

                using (var store = db.NewStore())
                {
                    ApplyPrefillConventions(store, seedType);
                    store.Initialize();

                    sagas = await Prefill(store, seedType);
                }

                // Need to ensure multiple runs will work, after conventions document is stored
                for (var i = 0; i < 3; i++)
                {
                    using (var store = db.NewStore())
                    {
                        Console.WriteLine($"Testing saga lookups with DocumentStore initially configured for {seedType} conventions.");
                        ApplyTestConventions(store, seedType);
                        store.Initialize();

                        foreach (var saga in sagas)
                        {
                            var persister = new SagaPersister();
                            Console.WriteLine($"Retrieving SagaId {saga.Id} by SagaId");
                            TestSagaData byId;
                            using (var session = store.OpenAsyncSession())
                            {
                                byId = await persister.Get<TestSagaData>(saga.Id, new RavenDBSynchronizedStorageSession(session, false), null);
                            }
                            Assert.IsNotNull(byId);
                            Assert.AreEqual(byId.Id, saga.Id);
                            Assert.AreEqual(byId.OrderId, saga.OrderId);
                            Assert.AreEqual(byId.OriginalMessageId, saga.OriginalMessageId);
                            Assert.AreEqual(byId.Originator, saga.Originator);
                            Assert.AreEqual(1, saga.Counter);

                            Console.WriteLine($"Retrieving SagaId {saga.Id} by Correlation Property OrderId={saga.OrderId}");
                            using (var session = store.OpenAsyncSession())
                            {
                                byId = await persister.Get<TestSagaData>("OrderId", saga.OrderId, new RavenDBSynchronizedStorageSession(session, false), new ContextBag());
                            }
                            Assert.IsNotNull(byId);
                            Assert.AreEqual(byId.Id, saga.Id);
                            Assert.AreEqual(byId.OrderId, saga.OrderId);
                            Assert.AreEqual(byId.OriginalMessageId, saga.OriginalMessageId);
                            Assert.AreEqual(byId.Originator, saga.Originator);
                            Assert.AreEqual(1, saga.Counter);
                        }
                    }
                }
            }
        }

        [TestCase(ConventionType.RavenDefault)]
        [TestCase(ConventionType.NSBDefault)]
        [TestCase(ConventionType.Customer)]
        public async Task TestStoringSagas(ConventionType seedType)
        {
            using (var db = new ReusableDB())
            {
                List<TestSagaData> sagas;

                using (var store = db.NewStore())
                {
                    ApplyPrefillConventions(store, seedType);
                    store.Initialize();

                    sagas = await Prefill(store, seedType);
                }

                using (var store = db.NewStore())
                {
                    Console.WriteLine($"Testing saga updates with DocumentStore initially configured for {seedType} conventions.");
                    ApplyTestConventions(store, seedType);
                    store.Initialize();

                    // Update each saga, once by id and once by correlation property
                    foreach (var saga in sagas)
                    {
                        var persister = new SagaPersister();
                        Console.WriteLine($"Retrieving SagaId {saga.Id} by SagaId");
                        using (var session = store.OpenAsyncSession())
                        {
                            var ravenDBSynchronizedStorageSession = new RavenDBSynchronizedStorageSession(session, false);
                            var byId = await persister.Get<TestSagaData>(saga.Id, ravenDBSynchronizedStorageSession, null);
                            Assert.IsNotNull(byId);

                            byId.Counter++;
                            await persister.Update(byId, ravenDBSynchronizedStorageSession, null);
                            await session.SaveChangesAsync();

                            Console.WriteLine($"Retrieving SagaId {saga.Id} by Correlation Property OrderId={saga.OrderId}");
                            var byOrderId = await persister.Get<TestSagaData>("OrderId", saga.OrderId, ravenDBSynchronizedStorageSession, new ContextBag());
                            Assert.IsNotNull(byOrderId);

                            byOrderId.Counter++;
                            await persister.Update(byOrderId, ravenDBSynchronizedStorageSession, null);
                            await session.SaveChangesAsync();
                        }
                    }

                    Console.WriteLine("Retrieving each saga again by SagaId and making sure Counter == 3");
                    foreach (var saga in sagas)
                    {
                        var persister = new SagaPersister();
                        using (var session = store.OpenAsyncSession())
                        {
                            Console.WriteLine($"Retrieving SagaId {saga.Id} by SagaId");
                            var byId = await persister.Get<TestSagaData>(saga.Id, new RavenDBSynchronizedStorageSession(session, false), null);
                            Assert.IsNotNull(byId);
                            Assert.AreEqual(3, byId.Counter);
                        }
                    }
                }
            }
        }

        [TestCase(ConventionType.RavenDefault)]
        [TestCase(ConventionType.NSBDefault)]
        [TestCase(ConventionType.Customer)]
        public async Task TestMarkingAsComplete(ConventionType seedType)
        {
            using (var db = new ReusableDB())
            {
                List<TestSagaData> sagas;

                using (var store = db.NewStore())
                {
                    ApplyPrefillConventions(store, seedType);
                    store.Initialize();

                    sagas = await Prefill(store, seedType);
                }

                using (var store = db.NewStore())
                {
                    Console.WriteLine($"Testing saga lookups with DocumentStore initially configured for {seedType} conventions.");
                    ApplyTestConventions(store, seedType);
                    store.Initialize();

                    // Update each saga, once by id and once by correlation property
                    foreach (var saga in sagas)
                    {
                        var persister = new SagaPersister();
                        using (var session = store.OpenAsyncSession())
                        {
                            Console.WriteLine($"Retrieving SagaId {saga.Id} by Correlation Property OrderId={saga.OrderId} and completing.");
                            var ravenDBSynchronizedStorageSession = new RavenDBSynchronizedStorageSession(session, false);
                            var contextBag = new ContextBag();
                            var byOrderId = await persister.Get<TestSagaData>("OrderId", saga.OrderId, ravenDBSynchronizedStorageSession, contextBag);
                            Assert.IsNotNull(byOrderId);

                            await persister.Complete(byOrderId, ravenDBSynchronizedStorageSession, contextBag);
                            await session.SaveChangesAsync();
                        }
                    }

                    db.WaitForIndexing(store);

                    // Ensure terms are still the saga type and unique identity type
                    var terms = store.DatabaseCommands.GetTerms("Raven/DocumentsByEntityName", "Tag", null, 1024).ToList();
                    Assert.AreEqual(2, terms.Count);

                    foreach (var term in terms)
                    {
                        var query = new IndexQuery
                        {
                            Query = "Tag:" + term,
                            PageSize = 0
                        };

                        // Ensure there are none left
                        var queryResult = store.DatabaseCommands.Query("Raven/DocumentsByEntityName", query);
                        Assert.AreEqual(0, queryResult.TotalResults);
                    }
                }
            }
        }

        private async Task<List<TestSagaData>> Prefill(IDocumentStore store, ConventionType seedType)
        {
            var snooper = StoreSnooper.Install((DocumentStore)store);

            Console.WriteLine($"Filling database with saga data, using {seedType} conventions");

            var names = new Dictionary<ConventionType, string>
            {
                {ConventionType.RavenDefault, "TestSagaDatas"},
                {ConventionType.NSBDefault, "TestSaga"},
                {ConventionType.Customer, "ataDagaStseT"}
            };
            var name = names[seedType];

            var sagas = Enumerable.Range(1001, 10)
                .Select(orderId => new TestSagaData
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    OriginalMessageId = Guid.NewGuid().ToString(),
                    Originator = "SomeSaga",
                    Counter = 1
                })
                .ToList();


            foreach (var saga in sagas)
            {
                snooper.Clear();

                var persister = new SagaPersister();

                Console.WriteLine($"Storing SagaId {saga.Id}");
                using (var session = store.OpenAsyncSession())
                {
                    await persister.Save(saga, new SagaCorrelationProperty("OrderId", saga.OrderId), new RavenDBSynchronizedStorageSession(session, false), null);
                    await session.SaveChangesAsync();
                }
                var keysStored = snooper.KeysStored.ToList();

                foreach (var key in keysStored)
                {
                    Console.WriteLine($"  * Document created: {key}");
                }

                Assert.AreEqual(2, snooper.KeyCount);
                Assert.Contains($"{name}/{saga.Id}", keysStored);
            }

            return sagas;
        }
    }

    class TestSagaData : ContainSagaData
    {
        public int OrderId { get; set; }
        public int Counter { get; set; }
    }
}