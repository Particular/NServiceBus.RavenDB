namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.Saga;
    using NServiceBus.TimeoutPersisters.RavenDB;
    using NUnit.Framework;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Json.Linq;
    using Raven.Tests.Helpers;

    [TestFixture]
    public class InconsistentLegacyIdsCheck : RavenTestBase
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestTimeoutConventions(bool storeWithModifiedConventions, bool readWithModifiedConventions)
        {
            var sagaId = Guid.NewGuid();
            var docStore = CreateFilledDocStore(sagaId, storeWithModifiedConventions, readWithModifiedConventions);

            using (var session = docStore.OpenSession())
            {
                var timeout = session.Load<TimeoutData>(1001);
                Assert.IsNotNull(timeout);
                Assert.AreEqual("TheOwningTimeoutManager", timeout.OwningTimeoutManager);
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestSagaConventions(bool storeWithModifiedConventions, bool readWithModifiedConventions)
        {
            var sagaId = Guid.NewGuid();
            var docStore = CreateFilledDocStore(sagaId, storeWithModifiedConventions, readWithModifiedConventions);

            using (var session = docStore.OpenSession())
            {
                var saga = session.Load<TestSagaData>(sagaId);
                Assert.IsNotNull(saga);
                Assert.AreEqual(42, saga.OrderId);
                Assert.AreEqual("SomeSaga", saga.Originator);
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestStoringSaga(bool storeWithModifiedConventions, bool readWithModifiedConventions)
        {
            var docStore = CreateFilledDocStore(Guid.NewGuid(), storeWithModifiedConventions, readWithModifiedConventions);
            var snooper = StoreSnooper.Install(docStore);

            var newSagaId = Guid.NewGuid();

            using (var session = docStore.OpenSession())
            {
                session.Store(new TestSagaData
                {
                    Id = newSagaId,
                    OrderId = 12345,
                    OriginalMessageId = Guid.NewGuid().ToString(),
                    Originator = "TheOriginator"
                });
                session.SaveChanges();
            }

            var desiredCollection = storeWithModifiedConventions ? "TestSaga" : "TestSagaDatas";
            var desiredId = $"{desiredCollection}/{newSagaId}";

            Assert.AreEqual(1, snooper.KeysStored.Count());
            Assert.AreEqual(desiredId, snooper.KeysStored.First());
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestStoringTimeout(bool storeWithModifiedConventions, bool readWithModifiedConventions)
        {
            var docStore = CreateFilledDocStore(Guid.NewGuid(), storeWithModifiedConventions, readWithModifiedConventions);
            var snooper = StoreSnooper.Install(docStore);

            using (var session = docStore.OpenSession())
            {
                session.Store(new TimeoutData
                { 
                    Destination = new Address("Somewhere", "OutThere"),
                    Headers = new Dictionary<string, string>(),
                    OwningTimeoutManager = "TheOwner",
                    SagaId = Guid.Empty,
                    State = new byte[0],
                    Time = DateTime.UtcNow.AddDays(5)
                });
                session.SaveChanges();
            }

            var desiredCollection = storeWithModifiedConventions ? "TimeoutData" : "TimeoutDatas";
            var desiredId = $"{desiredCollection}/1";

            Assert.AreEqual(1, snooper.KeysStored.Count());
            Assert.AreEqual(desiredId, snooper.KeysStored.First());
        }

        private IDocumentStore CreateFilledDocStore(Guid sagaId, bool storeWithModifiedConventions, bool readWithModifiedConventions)
        {
            var store = NewDocumentStore(configureStore: s =>
            {
                if (readWithModifiedConventions)
                {
                    s.Conventions.FindTypeTagName = BackwardsCompatibilityHelper.LegacyFindTypeTagName;
                }

                var documentIdConventions = new DocumentIdConventions(s, new [] { typeof(TestSagaData) });
                s.Conventions.FindTypeTagName = documentIdConventions.FindTypeTagName;
            }).Initialize();

            var timeout = new TimeoutData
            {
                Destination = new Address("FakeQueue", "FakeMachine"),
                Headers = new Dictionary<string, string>(),
                OwningTimeoutManager = "TheOwningTimeoutManager"
            };

            var saga = new TestSagaData
            {
                Id = sagaId,
                OrderId = 42,
                OriginalMessageId = Guid.NewGuid().ToString(),
                Originator = "SomeSaga"
            };

            if (storeWithModifiedConventions)
            {
                DirectStore(store, $"TestSaga/{sagaId}", saga, "TestSaga");
                DirectStore(store, "TimeoutData/1001", timeout, "TimeoutData");
            }
            else
            {
                DirectStore(store, $"TestSagaDatas/{sagaId}", saga, "TestSagaDatas");
                DirectStore(store, "TimeoutDatas/1001", timeout, "TimeoutDatas");
            }

            while (store.DatabaseCommands.GetStatistics().StaleIndexes.Length != 0)
            {
                Thread.Sleep(10);
            }

            return store;
        }

        private void DirectStore(IDocumentStore store, string id, object doc, string entityName)
        {
            var jsonDoc = RavenJObject.FromObject(doc);
            var metadata = new RavenJObject();
            metadata["Raven-Entity-Name"] = entityName;
            var type = doc.GetType();
            metadata["Raven-Clr-Type"] = $"{type.FullName}, {type.Assembly.GetName().Name}";

            store.DatabaseCommands.Put(id, Etag.Empty, jsonDoc, metadata);
        }

        class TestSagaData : ContainSagaData
        {
            [Unique]
            public int OrderId { get; set; }
        }
    }
}
