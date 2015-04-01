using System;
using System.Collections.Generic;

namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using NUnit.Framework;
    using Support;
    using TimeoutPersisters.RavenDB;
    using TimeoutData = Timeout.Core.TimeoutData;
    using Timeout = TimeoutPersisters.RavenDB.TimeoutData;

    [TestFixture]
    [Ignore("These tests currently operate under the assumption TimeoutData.Id gets assigned by the persistence layer; need to revisit this")]
    class When_removing_timeouts_from_the_storage:RavenDBPersistenceTestBase
    {
        [Test]
        public void Should_return_the_correct_headers()
        {
            var persister = new TimeoutPersister
            {
                DocumentStore = store,
                EndpointName = "MyTestEndpoint",
            };

            var headers = new Dictionary<string, string>
                          {
                              {"Bar", "34234"},
                              {"Foo", "aString1"},
                              {"Super", "aString2"}
                          };

            var timeout = new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                Destination = "timeouts@" + RuntimeEnvironment.MachineName,
                SagaId = Guid.NewGuid(),
                State = new byte[] { 1, 1, 133, 200 },
                Headers = headers,
                OwningTimeoutManager = "MyTestEndpoint",
            };
            persister.Add(timeout);

            TimeoutData timeoutData;
            persister.TryRemove(timeout.Id, out timeoutData);

            CollectionAssert.AreEqual(headers, timeoutData.Headers);
        }

        [Test]
        public void Should_remove_timeouts_by_id()
        {
            new TimeoutsIndex().Execute(store);

            var persister = new TimeoutPersister
            {
                DocumentStore = store,
                EndpointName = "MyTestEndpoint",
            };

            var t1 = new TimeoutData
            {
                Time = DateTime.Now.AddYears(-1),
                OwningTimeoutManager = "MyTestEndpoint",
                Headers = new Dictionary<string, string>
                                   {
                                       {"Header1", "Value1"}
                                   }
            };
            var t2 = new TimeoutData
            {
                Time = DateTime.Now.AddYears(-1),
                OwningTimeoutManager = "MyTestEndpoint",
                Headers = new Dictionary<string, string>
                                   {
                                       {"Header1", "Value1"}
                                   }
            };

            persister.Add(t1);
            persister.Add(t2);

            WaitForIndexing(store);

            DateTime nextTimeToRunQuery;
            var timeouts = persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);

            foreach (var timeout in timeouts)
            {
                TimeoutData timeoutData;
                persister.TryRemove(timeout.Item1, out timeoutData);
            }

            using (var session = store.OpenSession())
            {
                Assert.Null(session.Load<Timeout>(new Guid(t1.Id)));
                Assert.Null(session.Load<Timeout>(new Guid(t2.Id)));
            }
        }

        [Test]
        public void Should_remove_timeouts_by_sagaid()
        {
            new TimeoutsIndex().Execute(store);

            var persister = new TimeoutPersister
            {
                DocumentStore = store,
                EndpointName = "MyTestEndpoint",
            };

            var sagaId1 = Guid.NewGuid();
            var sagaId2 = Guid.NewGuid();
            var t1 = new TimeoutData
            {
                SagaId = sagaId1,
                Time = DateTime.Now.AddYears(1),
                OwningTimeoutManager = "MyTestEndpoint",
                Headers = new Dictionary<string, string>
                                   {
                                       {"Header1", "Value1"}
                                   }
            };
            var t2 = new TimeoutData
            {
                SagaId = sagaId2,
                Time = DateTime.Now.AddYears(1),
                OwningTimeoutManager = "MyTestEndpoint",
                Headers = new Dictionary<string, string>
                                   {
                                       {"Header1", "Value1"}
                                   }
            };

            persister.Add(t1);
            persister.Add(t2);

            WaitForIndexing(store);

            persister.RemoveTimeoutBy(sagaId1);
            persister.RemoveTimeoutBy(sagaId2);

            using (var session = store.OpenSession())
            {
                Assert.Null(session.Load<Timeout>(new Guid(t1.Id)));
                Assert.Null(session.Load<Timeout>(new Guid(t2.Id)));
            }
        }
    }
}
