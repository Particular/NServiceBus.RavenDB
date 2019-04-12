namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using TimeoutData = NServiceBus.Timeout.Core.TimeoutData;

    [TestFixture]
    public class Should_not_skip_timeouts
    {
        [TestCase]
        [Explicit]
        public async Task Never_ever()
        {
            var db = Guid.NewGuid().ToString();
            using (var documentStore = new DocumentStore
            {
                Urls = TestConstants.RavenUrls,
                Database = db
            }.Initialize())
            {
                new TimeoutsIndex().Execute(documentStore);

                var query = new QueryTimeouts(documentStore, "foo")
                {
                    TriggerCleanupEvery = TimeSpan.FromHours(1) // Make sure cleanup doesn't run automatically
                };
                var persister = new TimeoutPersister(documentStore);
                var context = new ContextBag();

                var startSlice = DateTime.UtcNow.AddYears(-10);
                // avoid cleanup from running during the test by making it register as being run
                Assert.AreEqual(0, (await query.GetCleanupChunk(startSlice)).Count());

                var expected = new List<Tuple<string, DateTime>>();
                var lastTimeout = DateTime.UtcNow;
                var finishedAdding = false;

                new Thread(() =>
                {
                    var sagaId = Guid.NewGuid();
                    for (var i = 0; i < 10000; i++)
                    {
                        var td = new TimeoutData
                        {
                            SagaId = sagaId,
                            Destination = "queue@machine",
                            Time = DateTime.UtcNow.AddSeconds(RandomProvider.GetThreadRandom().Next(1, 20)),
                            OwningTimeoutManager = string.Empty
                        };
                        persister.Add(td, context).Wait();
                        expected.Add(new Tuple<string, DateTime>(td.Id, td.Time));
                        lastTimeout = (td.Time > lastTimeout) ? td.Time : lastTimeout;
                    }
                    finishedAdding = true;
                    Trace.WriteLine("*** Finished adding ***");
                }).Start();

                // Mimic the behavior of the TimeoutPersister coordinator
                var found = 0;
                while (!finishedAdding || startSlice < lastTimeout)
                {
                    var timeoutData = await query.GetNextChunk(startSlice);
                    foreach (var timeout in timeoutData.DueTimeouts)
                    {
                        if (startSlice < timeout.DueTime)
                        {
                            startSlice = timeout.DueTime;
                        }

                        Assert.True(await persister.TryRemove(timeout.Id, context));
                        found++;
                    }
                }

                WaitForIndexing(documentStore);

                // If the persister reports stale results have been seen at one point during its normal operation,
                // we need to perform manual cleaup.
                while (true)
                {
                    var chunkToCleanup = (await query.GetCleanupChunk(DateTime.UtcNow.AddDays(1))).ToArray();
                    if (chunkToCleanup.Length == 0)
                    {
                        break;
                    }

                    found += chunkToCleanup.Length;
                    foreach (var tuple in chunkToCleanup)
                    {
                        Assert.True(await persister.TryRemove(tuple.Id, context));
                    }

                    WaitForIndexing(documentStore);
                }

                using (var session = documentStore.OpenAsyncSession())
                {
                    var results = await session.Query<TimeoutData>().ToListAsync();
                    Assert.AreEqual(0, results.Count);
                }

                Assert.AreEqual(expected.Count, found);
            }
        }

        [TestCase]
        [Explicit]
        public async Task Should_not_skip_timeouts_also_with_multiple_clients_adding_timeouts()
        {
            var db = Guid.NewGuid().ToString();
            using (var documentStore = new DocumentStore
            {
                Urls = TestConstants.RavenUrls,
                Database = db
            }.Initialize())
            {
                new TimeoutsIndex().Execute(documentStore);

                var query = new QueryTimeouts(documentStore, "foo")
                {
                    TriggerCleanupEvery = TimeSpan.FromDays(1) // Make sure cleanup doesn't run automatically
                };
                var persister = new TimeoutPersister(documentStore);
                var context = new ContextBag();

                var startSlice = DateTime.UtcNow.AddYears(-10);
                // avoid cleanup from running during the test by making it register as being run
                Assert.AreEqual(0, (await query.GetCleanupChunk(startSlice)).Count());

                const int insertsPerThread = 1000;
                var expected = 0;
                var lastExpectedTimeout = DateTime.UtcNow;
                var finishedAdding1 = false;
                var finishedAdding2 = false;

                new Thread(() =>
                {
                    var sagaId = Guid.NewGuid();
                    for (var i = 0; i < insertsPerThread; i++)
                    {
                        var td = new TimeoutData
                        {
                            SagaId = sagaId,
                            Destination = "queue@machine",
                            Time = DateTime.UtcNow.AddSeconds(RandomProvider.GetThreadRandom().Next(1, 20)),
                            OwningTimeoutManager = string.Empty
                        };
                        persister.Add(td, context).Wait();
                        Interlocked.Increment(ref expected);
                        lastExpectedTimeout = (td.Time > lastExpectedTimeout) ? td.Time : lastExpectedTimeout;
                    }
                    finishedAdding1 = true;
                    Console.WriteLine("*** Finished adding ***");
                }).Start();

                new Thread(() =>
                {
                    using (var store = new DocumentStore
                    {
                        Urls = TestConstants.RavenUrls,
                        Database = db
                    }.Initialize())
                    {
                        var persister2 = new TimeoutPersister(store);

                        var sagaId = Guid.NewGuid();
                        for (var i = 0; i < insertsPerThread; i++)
                        {
                            var td = new TimeoutData
                            {
                                SagaId = sagaId,
                                Destination = "queue@machine",
                                Time = DateTime.UtcNow.AddSeconds(RandomProvider.GetThreadRandom().Next(1, 20)),
                                OwningTimeoutManager = string.Empty
                            };
                            persister2.Add(td, context).Wait();
                            Interlocked.Increment(ref expected);
                            lastExpectedTimeout = (td.Time > lastExpectedTimeout) ? td.Time : lastExpectedTimeout;
                        }
                    }
                    finishedAdding2 = true;
                    Console.WriteLine("*** Finished adding via a second client connection ***");
                }).Start();

                // Mimic the behavior of the TimeoutPersister coordinator
                var found = 0;
                while (!finishedAdding1 || !finishedAdding2 || startSlice < lastExpectedTimeout)
                {
                    var timeoutDatas = await query.GetNextChunk(startSlice);
                    foreach (var timeoutData in timeoutDatas.DueTimeouts)
                    {
                        if (startSlice < timeoutData.DueTime)
                        {
                            startSlice = timeoutData.DueTime;
                        }

                        Assert.True(await persister.TryRemove(timeoutData.Id, context));
                        found++;
                    }
                }

                WaitForIndexing(documentStore);

                // If the persister reports stale results have been seen at one point during its normal operation,
                // we need to perform manual cleaup.
                while (true)
                {
                    var chunkToCleanup = (await query.GetCleanupChunk(DateTime.UtcNow.AddDays(1))).ToArray();
                    Console.WriteLine("Cleanup: got a chunk of size " + chunkToCleanup.Length);
                    if (chunkToCleanup.Length == 0)
                    {
                        break;
                    }

                    found += chunkToCleanup.Length;
                    foreach (var tuple in chunkToCleanup)
                    {
                        Assert.True(await persister.TryRemove(tuple.Id, context));
                    }

                    WaitForIndexing(documentStore);
                }

                using (var session = documentStore.OpenAsyncSession())
                {
                    var results = await session.Query<TimeoutData>().ToListAsync();
                    Assert.AreEqual(0, results.Count);
                }

                Assert.AreEqual(expected, found);
            }
        }

        static void WaitForIndexing(IDocumentStore store, string db = null, TimeSpan? timeout = null)
        {
            var databaseCommands = store.DatabaseCommands;
            if (db != null)
            {
                databaseCommands = databaseCommands.ForDatabase(db);
            }
            var spinUntil = SpinWait.SpinUntil(() => databaseCommands.GetStatistics().StaleIndexes.Length == 0, timeout ?? TimeSpan.FromSeconds(20));
            Assert.True(spinUntil);
        }

        static class RandomProvider
        {
            static int seed = Environment.TickCount;

            static ThreadLocal<Random> randomWrapper = new ThreadLocal<Random>(() =>
                new Random(Interlocked.Increment(ref seed))
                );

            public static Random GetThreadRandom()
            {
                return randomWrapper.Value;
            }
        }
    }
}