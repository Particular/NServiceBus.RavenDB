using System;
using System.Collections.Generic;

namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System.Linq;
    using NUnit.Framework;
    using Support;
    using TimeoutPersisters.RavenDB;
    using TimeoutData = Timeout.Core.TimeoutData;
    using Timeout = TimeoutPersisters.RavenDB.TimeoutData;

    [TestFixture]
    class When_removing_timeouts_from_the_storage : RavenTestBase
    {
        [Test]
        public void Should_return_the_correct_headers()
        {
            new TimeoutsIndex().Execute(store);

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
                Time = DateTime.UtcNow.AddYears(-1),
                Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                SagaId = Guid.NewGuid(),
                State = new byte[] { 1, 1, 133, 200 },
                Headers = headers,
                OwningTimeoutManager = "MyTestEndpoint",
            };
            persister.Add(timeout);

            WaitForIndexing(store);
            DateTime nextRun;

            var timeouts = persister.GetNextChunk(DateTime.Now.AddYears(-3), out nextRun).ToList();
            Assert.AreEqual(1, timeouts.Count());

            TimeoutData timeoutData;
            persister.TryRemove(timeouts.First().Item1, out timeoutData);

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
            var timeouts = persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery).ToList();
            Assert.AreEqual(2, timeouts.Count());

            foreach (var timeout in timeouts)
            {
                TimeoutData timeoutData;
                persister.TryRemove(timeout.Item1, out timeoutData);
            }

            foreach (var timeout in timeouts)
            {
                using (var session = store.OpenSession())
                {
                    Assert.Null(session.Load<Timeout>(timeout.Item1));
                }
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
                Time = DateTime.Now.AddYears(-1),
                OwningTimeoutManager = "MyTestEndpoint",
                Headers = new Dictionary<string, string>
                                   {
                                       {"Header1", "Value1"}
                                   }
            };
            var t2 = new TimeoutData
            {
                SagaId = sagaId2,
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
            var timeouts = persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery).ToList();
            Assert.AreEqual(2, timeouts.Count());

            persister.RemoveTimeoutBy(sagaId1);
            persister.RemoveTimeoutBy(sagaId2);

            foreach (var timeout in timeouts)
            {
                using (var session = store.OpenSession())
                {
                    Assert.Null(session.Load<Timeout>(timeout.Item1));
                }
            }
        }
    }
}
