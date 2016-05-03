namespace NServiceBus.RavenDB.Tests.DocumentIds
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using NServiceBus.Saga;
    using NUnit.Framework;
    using Raven.Abstractions.Data;
    using Raven.Client;

    [TestFixture]
    public class InconsistentSagaIdConventions : DocumentIdConventionTestBase
    {
        [TestCase(ConventionType.RavenDefault)]
        [TestCase(ConventionType.NSBDefault)]
        [TestCase(ConventionType.Customer)]
        public void TestRetrievingSagas(ConventionType seedType)
        {
            using (var db = new ReusableDB())
            {
                List<TestSagaData> sagas;

                using (var store = db.NewStore())
                {
                    ApplyPrefillConventions(store, seedType);
                    store.Initialize();

                    sagas = Prefill(store, seedType);
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
                            var storeAccessor = new StoreAccessor(store);
                            var sessionFactory = new RavenSessionFactory(storeAccessor);
                            var persister = new RavenSagaPersister(sessionFactory);
                            using (sessionFactory.Session)
                            {
                                Console.WriteLine($"Retrieving SagaId {saga.Id} by SagaId");
                                var byId = persister.Get<TestSagaData>(saga.Id);
                                Assert.IsNotNull(byId);
                                Assert.AreEqual(byId.Id, saga.Id);
                                Assert.AreEqual(byId.OrderId, saga.OrderId);
                                Assert.AreEqual(byId.OriginalMessageId, saga.OriginalMessageId);
                                Assert.AreEqual(byId.Originator, saga.Originator);
                                Assert.AreEqual(1, saga.Counter);
                            }
                            sessionFactory.ReleaseSession();

                            using (sessionFactory.Session)
                            {
                                Console.WriteLine($"Retrieving SagaId {saga.Id} by Correlation Property OrderId={saga.OrderId}");
                                var byId = persister.Get<TestSagaData>("OrderId", saga.OrderId);
                                Assert.IsNotNull(byId);
                                Assert.AreEqual(byId.Id, saga.Id);
                                Assert.AreEqual(byId.OrderId, saga.OrderId);
                                Assert.AreEqual(byId.OriginalMessageId, saga.OriginalMessageId);
                                Assert.AreEqual(byId.Originator, saga.Originator);
                                Assert.AreEqual(1, saga.Counter);
                            }
                            sessionFactory.ReleaseSession();
                        }
                    }
                }
            }
        }

        [TestCase(ConventionType.RavenDefault)]
        [TestCase(ConventionType.NSBDefault)]
        [TestCase(ConventionType.Customer)]
        public void TestStoringSagas(ConventionType seedType)
        {
            using (var db = new ReusableDB())
            {
                List<TestSagaData> sagas;

                using (var store = db.NewStore())
                {
                    ApplyPrefillConventions(store, seedType);
                    store.Initialize();

                    sagas = Prefill(store, seedType);
                }

                using (var store = db.NewStore())
                {
                    Console.WriteLine($"Testing saga updates with DocumentStore initially configured for {seedType} conventions.");
                    ApplyTestConventions(store, seedType);
                    store.Initialize();

                    // Update each saga, once by id and once by correlation property
                    foreach (var saga in sagas)
                    {
                        var storeAccessor = new StoreAccessor(store);
                        var sessionFactory = new RavenSessionFactory(storeAccessor);
                        var persister = new RavenSagaPersister(sessionFactory);
                        using (sessionFactory.Session)
                        {
                            Console.WriteLine($"Retrieving SagaId {saga.Id} by SagaId");
                            var byId = persister.Get<TestSagaData>(saga.Id);
                            Assert.IsNotNull(byId);

                            byId.Counter++;
                            persister.Update(byId);
                            sessionFactory.SaveChanges();
                        }
                        sessionFactory.ReleaseSession();

                        using (sessionFactory.Session)
                        {
                            Console.WriteLine($"Retrieving SagaId {saga.Id} by Correlation Property OrderId={saga.OrderId}");
                            var byOrderId = persister.Get<TestSagaData>("OrderId", saga.OrderId);
                            Assert.IsNotNull(byOrderId);

                            byOrderId.Counter++;
                            persister.Update(byOrderId);
                            sessionFactory.SaveChanges();
                        }
                        sessionFactory.ReleaseSession();
                    }

                    Console.WriteLine("Retrieving each saga again by SagaId and making sure Counter == 3");
                    foreach (var saga in sagas)
                    {
                        var storeAccessor = new StoreAccessor(store);
                        var sessionFactory = new RavenSessionFactory(storeAccessor);
                        var persister = new RavenSagaPersister(sessionFactory);
                        using (sessionFactory.Session)
                        {
                            Console.WriteLine($"Retrieving SagaId {saga.Id} by SagaId");
                            var byId = persister.Get<TestSagaData>(saga.Id);
                            Assert.IsNotNull(byId);
                            Assert.AreEqual(3, byId.Counter);
                        }
                        sessionFactory.ReleaseSession();
                    }
                }
            }
        }

        [TestCase(ConventionType.RavenDefault)]
        [TestCase(ConventionType.NSBDefault)]
        [TestCase(ConventionType.Customer)]
        public void TestMarkingAsComplete(ConventionType seedType)
        {
            using (var db = new ReusableDB())
            {
                List<TestSagaData> sagas;

                using (var store = db.NewStore())
                {
                    ApplyPrefillConventions(store, seedType);
                    store.Initialize();

                    sagas = Prefill(store, seedType);
                }

                using (var store = db.NewStore())
                {
                    Console.WriteLine($"Testing saga lookups with DocumentStore initially configured for {seedType} conventions.");
                    ApplyTestConventions(store, seedType);
                    store.Initialize();

                    // Update each saga, once by id and once by correlation property
                    foreach (var saga in sagas)
                    {
                        var storeAccessor = new StoreAccessor(store);
                        var sessionFactory = new RavenSessionFactory(storeAccessor);
                        var persister = new RavenSagaPersister(sessionFactory);
                        using (sessionFactory.Session)
                        {
                            Console.WriteLine($"Retrieving SagaId {saga.Id} by Correlation Property OrderId={saga.OrderId} and completing.");
                            var byOrderId = persister.Get<TestSagaData>("OrderId", saga.OrderId);
                            Assert.IsNotNull(byOrderId);

                            persister.Complete(byOrderId);
                            sessionFactory.SaveChanges();
                        }
                        sessionFactory.ReleaseSession();
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
                        var queryResult = store.DatabaseCommands.Query("Raven/DocumentsByEntityName", query, null);
                        Assert.AreEqual(0, queryResult.TotalResults);
                    }
                }
            }
        }

        private List<TestSagaData> Prefill(IDocumentStore store, ConventionType seedType)
        {
            var snooper = StoreSnooper.Install(store);

            Console.WriteLine($"Filling database with saga data, using {seedType} conventions");

            var names = new Dictionary<ConventionType, string>
            {
                { ConventionType.RavenDefault, "TestSagaDatas" },
                { ConventionType.NSBDefault, "TestSaga" },
                { ConventionType.Customer, "ataDagaStseT" }
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

                var storeAccessor = new StoreAccessor(store);
                var sessionFactory = new RavenSessionFactory(storeAccessor);
                var persister = new RavenSagaPersister(sessionFactory);
                using (sessionFactory.Session)
                {
                    Console.WriteLine($"Storing SagaId {saga.Id}");
                    persister.Save(saga);
                    sessionFactory.SaveChanges();
                }
                sessionFactory.ReleaseSession();

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
        [Unique]
        public int OrderId { get; set; }
        public int Counter { get; set; }
    }
}