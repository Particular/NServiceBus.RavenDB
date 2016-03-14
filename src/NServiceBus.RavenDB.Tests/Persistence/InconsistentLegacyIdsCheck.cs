namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using System.Collections.Generic;
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
                var timeout = session.Load<TimeoutData>(1);
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

        private IDocumentStore CreateFilledDocStore(Guid sagaId, bool useLegacyDocIdConventions, bool readWithModifiedConventions)
        {
            var store = NewDocumentStore(configureStore: s =>
            {
                if (readWithModifiedConventions)
                {
                    s.Conventions.FindTypeTagName = BackwardsCompatibilityHelper.LegacyFindTypeTagName;
                }
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

            if (useLegacyDocIdConventions)
            {
                DirectStore(store, $"TestSaga/{sagaId}", saga, "TestSaga");
                DirectStore(store, "TimeoutData/1", timeout, "TimeoutData");
            }
            else
            {
                DirectStore(store, $"TestSagaDatas/{sagaId}", saga, "TestSagaDatas");
                DirectStore(store, "TimeoutDatas/1", timeout, "TimeoutDatas");
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
