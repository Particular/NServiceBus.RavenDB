using System;
using System.Collections.Generic;

namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Support;
    using TimeoutPersisters.RavenDB;
    using TimeoutData = Timeout.Core.TimeoutData;
    using Timeout = TimeoutPersisters.RavenDB.TimeoutData;

    [TestFixture]
    class When_removing_timeouts_from_the_storage : RavenTestBase
    {
        TimeoutPersister timeoutPersister;

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
            timeoutPersister.Add(GetTimeoutWithHeaders());

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
        public void Peek_should_return_the_correct_headers()
        {
            timeoutPersister.Add(GetTimeoutWithHeaders());

            var timeouts = GetTimeouts();
            Assert.AreEqual(1, timeouts.Count());

            var timeoutData = timeoutPersister.Peek(timeouts.First().Item1);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                {"Bar", "34234"},
                {"Foo", "aString1"},
                {"Super", "aString2"}
            }, timeoutData.Headers);
        }

        [Test]
        public void Peek_should_return_null_for_non_existing_timeout()
        {
            var timeoutData = timeoutPersister.Peek("TimeoutDatas/1");

            Assert.IsNull(timeoutData);
        }

        [Test]
        public void Should_remove_timeouts_by_id_using_old_interface()
        {
            timeoutPersister.Add(GetTimeoutWithHeaders());
            timeoutPersister.Add(GetTimeoutWithHeaders());
            
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
        public void Should_remove_timeouts_by_id_and_return_true_using_new_interface()
        {
            timeoutPersister.Add(GetTimeoutWithHeaders());
            timeoutPersister.Add(GetTimeoutWithHeaders());

            var timeouts = GetTimeouts();
            Assert.AreEqual(2, timeouts.Count());

            bool result = false;
            foreach (var timeout in timeouts)
            {
                result = timeoutPersister.TryRemove(timeout.Item1);
            }

            AssertAllTimeoutsHaveBeenRemoved(timeouts);
            Assert.IsTrue(result);
        }

        [Test]
        public void TryRemove_should_return_false_if_timeout_already_deleted()
        {
            timeoutPersister.Add(GetTimeoutWithHeaders());

            var timeouts = GetTimeouts();
            Assert.AreEqual(1, timeouts.Count());

            var timeoutId = timeouts.First().Item1;

            Assert.IsTrue(timeoutPersister.TryRemove(timeoutId));
            Assert.IsFalse(timeoutPersister.TryRemove(timeoutId));
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

        [Test]
        public void TryRemove_should_work_with_concurrent_operations()
        {
            timeoutPersister.Add(GetTimeoutWithHeaders());
            var timeouts = GetTimeouts();

            var t1 = Task.Run(() =>
            {
                using (var tx = new TransactionScope())
                {
                    var t1Remove = timeoutPersister.TryRemove(timeouts.First().Item1);
                    tx.Complete();
                    return t1Remove;
                }
            });

            var t2 = Task.Run(() =>
            {
                using (var tx = new TransactionScope())
                {
                    var t2Remove = timeoutPersister.TryRemove(timeouts.First().Item1);
                    tx.Complete();
                    return t2Remove;
                }
            });

            Assert.IsTrue(t1.Result || t2.Result);
            Assert.IsFalse(t1.Result && t2.Result);
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
            var timeoutWithHeaders1 = GetTimeoutWithHeaders();
            timeoutWithHeaders1.SagaId = sagaId;
            return timeoutWithHeaders1;
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
