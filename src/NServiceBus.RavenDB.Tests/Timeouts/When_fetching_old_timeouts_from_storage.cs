﻿namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Support;
    using NServiceBus.TimeoutPersisters.RavenDB;
    using NUnit.Framework;

    public class When_fetching_old_timeouts_from_storage : RavenDBPersistenceTestBase
    {
        QueryTimeouts query;

        public override void SetUp()
        {
            base.SetUp();

            store.Listeners.RegisterListener(new FakeLegacyTimoutDataClrTypeConversionListener());
            // for querying we don't need TimeoutDataV1toV2Converter

            new TimeoutsIndex().Execute(store);

            query = new QueryTimeouts(store, "MyTestEndpoint");
        }

        [Test]
        public async Task Should_return_the_complete_list_of_timeouts()
        {
            const int numberOfTimeoutsToAdd = 10;

            var session = store.OpenAsyncSession();
            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                await session.StoreAsync(new LegacyTimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(-1),
                    Destination = new LegacyAddress("timeouts", RuntimeEnvironment.MachineName),
                    SagaId = Guid.NewGuid(),
                    State = new byte[] { 0, 0, 133 },
                    Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                    OwningTimeoutManager = "MyTestEndpoint",
                });
            }
            await session.SaveChangesAsync();

            WaitForIndexing(store);

            Assert.AreEqual(numberOfTimeoutsToAdd, (await query.GetNextChunk(DateTime.UtcNow.AddYears(-3))).DueTimeouts.Count());
        }

        [Test]
        public async Task Should_return_the_complete_list_of_timeouts_even_when_mixed_old_and_new()
        {
            const int numberOfTimeoutsToAdd = 10;

            var session = store.OpenAsyncSession();
            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                if (i % 2 == 0)
                {
                    await session.StoreAsync(new LegacyTimeoutData
                    {
                        Time = DateTime.UtcNow.AddHours(-1),
                        Destination = new LegacyAddress("timeouts", RuntimeEnvironment.MachineName),
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
                        OwningTimeoutManager = "MyTestEndpoint",
                    });
                }
                else
                {
                    await session.StoreAsync(new TimeoutData
                    {
                        Time = DateTime.UtcNow.AddHours(-1),
                        Destination = "timeouts" + "@" + RuntimeEnvironment.MachineName,
                        SagaId = Guid.NewGuid(),
                        State = new byte[] { 0, 0, 133 },
                        Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                        OwningTimeoutManager = "MyTestEndpoint",
                    });
                }
            }
            await session.SaveChangesAsync();

            WaitForIndexing(store);

            Assert.AreEqual(numberOfTimeoutsToAdd, (await query.GetNextChunk(DateTime.UtcNow.AddYears(-3))).DueTimeouts.Count());
        }

        [Test]
        public async Task Should_return_the_next_time_of_retrieval()
        {
            query.CleanupGapFromTimeslice = TimeSpan.FromSeconds(1);
            query.TriggerCleanupEvery = TimeSpan.MinValue;

            var nextTime = DateTime.UtcNow.AddHours(1);

            var session = store.OpenAsyncSession();
            await session.StoreAsync(new LegacyTimeoutData
            {
                Time = nextTime,
                Destination = new LegacyAddress("timeouts", RuntimeEnvironment.MachineName),
                SagaId = Guid.NewGuid(),
                State = new byte[] { 0, 0, 133 },
                Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                OwningTimeoutManager = "MyTestEndpoint",
            });
            await session.SaveChangesAsync();

            WaitForIndexing(store);

            var nextTimeToRunQuery = (await query.GetNextChunk(DateTime.UtcNow.AddYears(-3))).NextTimeToQuery;

            Assert.IsTrue((nextTime - nextTimeToRunQuery).TotalSeconds < 1);
        }
    }
}