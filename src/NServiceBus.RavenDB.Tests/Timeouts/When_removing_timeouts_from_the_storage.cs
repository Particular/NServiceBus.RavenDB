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
        TimeoutPersister timeoutPersister;
        TimeoutData timeoutWithHeaders = GetTimeoutWithHeaders();

        [SetUp]
        public void SetUpTests()
        {
            new TimeoutsIndex().Execute(store);
            timeoutPersister = new TimeoutPersister
            {
                DocumentStore = store,
                EndpointName = "MyTestEndpoint",
            };
        }

        [Test]
        public void Should_return_the_correct_headers()
        {
            timeoutPersister.Add(timeoutWithHeaders);

            var timeouts = GetTimeouts();
            Assert.AreEqual(1, timeouts.Count());

            TimeoutData timeoutData;
            timeoutPersister.TryRemove(timeouts.First().Item1, out timeoutData);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                {"Bar", "34234"},
                {"Foo", "aString1"},
                {"Super", "aString2"}
            }, timeoutData.Headers);
        }

        [Test]
        public void Should_remove_timeouts_by_id()
        {
            timeoutPersister.Add(timeoutWithHeaders);
            timeoutPersister.Add(timeoutWithHeaders);
            
            var timeouts = GetTimeouts();
            Assert.AreEqual(2, timeouts.Count());

            foreach (var timeout in timeouts)
            {
                TimeoutData timeoutData;
                timeoutPersister.TryRemove(timeout.Item1, out timeoutData);
            }

            AssertAllTimeoutsHaveBeenRemoved(timeouts);
        }

        [Test]
        public void Should_remove_timeouts_by_sagaid()
        {
            var sagaId1 = Guid.NewGuid();
            var sagaId2 = Guid.NewGuid();

            timeoutPersister.Add(GetTimeoutWithSagaId(sagaId1));
            timeoutPersister.Add(GetTimeoutWithSagaId(sagaId2));
            
            var timeouts = GetTimeouts();
            Assert.AreEqual(2, timeouts.Count());

            timeoutPersister.RemoveTimeoutBy(sagaId1);
            timeoutPersister.RemoveTimeoutBy(sagaId2);

            AssertAllTimeoutsHaveBeenRemoved(timeouts);
        }

        static TimeoutData GetTimeoutWithHeaders()
        {
            return new TimeoutData
            {
                Time = DateTime.UtcNow.AddYears(-1),
                Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                SagaId = Guid.NewGuid(),
                State = new byte[] { 1, 1, 133, 200 },
                Headers = new Dictionary<string, string>
                {
                    {"Bar", "34234"},
                    {"Foo", "aString1"},
                    {"Super", "aString2"}
                },
                OwningTimeoutManager = "MyTestEndpoint",
            };
        }

        TimeoutData GetTimeoutWithSagaId(Guid sagaId)
        {
            timeoutWithHeaders.SagaId = sagaId;
            return timeoutWithHeaders;
        }

        List<Tuple<string, DateTime>> GetTimeouts()
        {
            WaitForIndexing(store);
            DateTime nextRun;
            var timeouts = timeoutPersister.GetNextChunk(DateTime.Now.AddYears(-3), out nextRun).ToList();
            return timeouts;
        }

        void AssertAllTimeoutsHaveBeenRemoved(List<Tuple<string, DateTime>> timeouts)
        {
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
