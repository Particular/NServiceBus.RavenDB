namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;
    using NServiceBus.Support;
    using NServiceBus.Timeout.Core;
    using NServiceBus.TimeoutPersisters.RavenDB;
    using NUnit.Framework;
    using TimeoutData = NServiceBus.Timeout.Core.TimeoutData;

    public class When_fetching_timeouts_from_storage : RavenDBPersistenceTestBase
    {
        QueryTimeouts query;
        TimeoutPersister persister;

        public override void SetUp()
        {
            base.SetUp();

            new TimeoutsIndex().Execute(store);

            persister = new TimeoutPersister(store);
            query = new QueryTimeouts(store)
            {
                EndpointName = "MyTestEndpoint",
            };
        }

        [Test]
        public void Should_return_the_complete_list_of_timeouts()
        {
            const int numberOfTimeoutsToAdd = 10;
            var options = new TimeoutPersistenceOptions(new ContextBag());
            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(-1),
                    Destination = "timeouts@" + RuntimeEnvironment.MachineName,
                    SagaId = Guid.NewGuid(),
                    State = new byte[] { 0, 0, 133 },
                    Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                    OwningTimeoutManager = "MyTestEndpoint",
                }, options);
            }

            WaitForIndexing(store);

            DateTime nextTimeToRunQuery;
            Assert.AreEqual(numberOfTimeoutsToAdd, query.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery).Count());            
        }

        [Test]
        public void Should_return_the_next_time_of_retrieval()
        {
            query.CleanupGapFromTimeslice = TimeSpan.FromSeconds(1);
            query.TriggerCleanupEvery = TimeSpan.MinValue;

            var nextTime = DateTime.UtcNow.AddHours(1);
            var options = new TimeoutPersistenceOptions(new ContextBag());

            persister.Add(new TimeoutData
            {
                Time = nextTime,
                Destination = "timeouts@" + RuntimeEnvironment.MachineName,
                SagaId = Guid.NewGuid(),
                State = new byte[] { 0, 0, 133 },
                Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                OwningTimeoutManager = "MyTestEndpoint",
            }, options);

            WaitForIndexing(store);

            DateTime nextTimeToRunQuery;
            query.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);

            Assert.IsTrue((nextTime - nextTimeToRunQuery).TotalSeconds < 1);
        }
    }
}
