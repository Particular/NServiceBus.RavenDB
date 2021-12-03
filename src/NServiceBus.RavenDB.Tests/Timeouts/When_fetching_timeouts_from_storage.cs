namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Support;
    using NUnit.Framework;
    using TimeoutData = Timeout.Core.TimeoutData;

    public class When_fetching_timeouts_from_storage : RavenDBPersistenceTestBase
    {
        public override async Task SetUp()
        {
            await base.SetUp();

            await new TimeoutsIndex().ExecuteAsync(store);

            persister = new TimeoutPersister(store, UseClusterWideTransactions);
            query = new QueryTimeouts(store, "MyTestEndpoint");
        }

        [Test]
        public async Task Should_return_the_complete_list_of_timeouts()
        {
            const int numberOfTimeoutsToAdd = 10;
            var context = new ContextBag();
            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                await persister.Add(new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(-1),
                    Destination = "timeouts@" + RuntimeEnvironment.MachineName,
                    SagaId = Guid.NewGuid(),
                    State = new byte[]
                    {
                        0,
                        0,
                        133
                    },
                    Headers = new Dictionary<string, string>
                    {
                        {"Bar", "34234"},
                        {"Foo", "aString1"},
                        {"Super", "aString2"}
                    },
                    OwningTimeoutManager = "MyTestEndpoint"
                }, context);
            }

            await WaitForIndexing();

            Assert.AreEqual(numberOfTimeoutsToAdd, (await query.GetNextChunk(DateTime.UtcNow.AddYears(-3))).DueTimeouts.Count());
        }

        [Test]
        public async Task Should_return_the_next_time_of_retrieval()
        {
            query.CleanupGapFromTimeslice = TimeSpan.FromSeconds(1);
            query.TriggerCleanupEvery = TimeSpan.Zero;

            var nextTime = DateTime.UtcNow.AddHours(1);
            var context = new ContextBag();

            await persister.Add(new TimeoutData
            {
                Time = nextTime,
                Destination = "timeouts@" + RuntimeEnvironment.MachineName,
                SagaId = Guid.NewGuid(),
                State = new byte[]
                {
                    0,
                    0,
                    133
                },
                Headers = new Dictionary<string, string>
                {
                    {"Bar", "34234"},
                    {"Foo", "aString1"},
                    {"Super", "aString2"}
                },
                OwningTimeoutManager = "MyTestEndpoint"
            }, context);

            await WaitForIndexing();

            var nextTimeToRunQuery = (await query.GetNextChunk(DateTime.UtcNow.AddYears(-3))).NextTimeToQuery;

            Assert.IsTrue((nextTime - nextTimeToRunQuery).TotalSeconds < 1);
        }

        TimeoutPersister persister;
        QueryTimeouts query;
    }
}