using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System.Diagnostics;
    using System.Threading;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Embedded;
    using Raven.Database.Util;
    using Timeout.Core;
    using TimeoutPersisters.RavenDB;
    using Timeout = TimeoutPersisters.RavenDB.Timeout;

    [TestFixture]
    public class Should_not_skip_timeouts
    {
        [TestCase]
        [Repeat(10)]
        public void Never_ever()
        {
            using (var documentStore = new EmbeddableDocumentStore
                                   {
                                       RunInMemory = true
                                   }.Initialize())
            {
                new TimeoutsIndex().Execute(documentStore);

                var persister = new TimeoutPersister
                            {
                                DocumentStore = documentStore,
                                EndpointName = "foo",
                                TriggerCleanupEvery = TimeSpan.FromSeconds(1),
                                CleanupGapFromTimeslice = TimeSpan.FromSeconds(2),
                            };

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
                                                Destination = new Address("queue", "machine"),
                                                Time = DateTime.UtcNow.AddSeconds(1 + RandomProvider.GetThreadRandom().Next(1, 20)),
                                                OwningTimeoutManager = string.Empty,
                                            };
                                   persister.Add(td);
                                   expected.Add(new Tuple<string, DateTime>(td.Id, td.Time));
                                   lastTimeout = (td.Time > lastTimeout) ? td.Time : lastTimeout;
                                   //Trace.WriteLine("Added timeout for " + td.Time);
                               }
                               finishedAdding = true;
                               Trace.WriteLine("*** Finished adding ***");
                           }).Start();

                // Mimic the behavior of the TimeoutPersister coordinator
                var found = 0;
                DateTime nextRetrieval;
                var startSlice = DateTime.UtcNow.AddYears(-10);
                while (!finishedAdding || startSlice < lastTimeout)
                {
                    var timeoutDatas = persister.GetNextChunk(startSlice, out nextRetrieval);
                    Trace.WriteLine("Querying for timeouts starting at " + startSlice + " with last known added timeout at " + lastTimeout);
                    foreach (var timeoutData in timeoutDatas)
                    {
                        if (startSlice < timeoutData.Item2)
                        {
                            startSlice = timeoutData.Item2;
                        }
                        found++;

                        TimeoutData tmptd;
                        Assert.IsTrue(persister.TryRemove(timeoutData.Item1, out tmptd));
                    }
                }

                var leftovers = persister.DoCleanup(lastTimeout.AddSeconds(5));
                foreach (var leftover in leftovers)
                {
                    TimeoutData tmptd;
                    persister.TryRemove(leftover.Item1, out tmptd);
                }
                Assert.AreEqual(expected.Count, found + leftovers.Count());

                WaitForIndexing(documentStore);

                using (var session = documentStore.OpenSession())
                {
                    var results = session.Query<Timeout, TimeoutsIndex>().ToList();
                    Assert.AreEqual(0, results.Count);
                }
            }
        }

        [TestCase, Explicit("This test has some weird behavior, need to figure it out")]
        public void Should_not_skip_timeouts_also_with_multiple_clients_adding_timeouts()
        {
            using (var documentStore = new DocumentStore {Url = "http://localhost:8080"}.Initialize())
            {
                new TimeoutsIndex().Execute(documentStore);

                var persister = new TimeoutPersister
                                {
                                    DocumentStore = documentStore,
                                    EndpointName = "foo",
                                    TriggerCleanupEvery = TimeSpan.FromSeconds(1),
                                    CleanupGapFromTimeslice = TimeSpan.FromSeconds(2),
                                };
                var expected = new ConcurrentSet<string>();
                var lastTimeout1 = DateTime.UtcNow;
                var lastTimeout2 = DateTime.UtcNow;
                var finishedAdding1 = false;
                var finishedAdding2 = false;
                new Thread(() =>
                           {
                               var sagaId = Guid.NewGuid();
                               for (var i = 0; i < 10000; i++)
                               {
                                   var td = new TimeoutData
                                            {
                                                SagaId = sagaId,
                                                Destination = new Address("queue", "machine"),
                                                Time = DateTime.UtcNow.AddSeconds(1 + RandomProvider.GetThreadRandom().Next(1, 20)),
                                                OwningTimeoutManager = string.Empty,
                                            };
                                   persister.Add(td);
                                   expected.Add(td.Id);
                                   lastTimeout1 = (td.Time > lastTimeout1) ? td.Time : lastTimeout1;
                                   //Trace.WriteLine("Added timeout for " + td.Time);
                               }
                               finishedAdding1 = true;
                               Trace.WriteLine("*** Finished adding ***");
                           }).Start();

                new Thread(() =>
                           {
                               using (var store = new DocumentStore{Url = "http://localhost:8080"}.Initialize())
                               {
                                   var persister2 = new TimeoutPersister
                                                    {
                                                        DocumentStore = store,
                                                        EndpointName = "bar",
                                                    };

                                   var sagaId = Guid.NewGuid();
                                   for (var i = 0; i < 10000; i++)
                                   {
                                       var td = new TimeoutData
                                                {
                                                    SagaId = sagaId,
                                                    Destination = new Address("queue", "machine"),
                                                    Time = DateTime.UtcNow.AddSeconds(1 + RandomProvider.GetThreadRandom().Next(1, 20)),
                                                    OwningTimeoutManager = string.Empty,
                                                };
                                       persister2.Add(td);
                                       expected.Add(td.Id);
                                       lastTimeout2 = (td.Time > lastTimeout2) ? td.Time : lastTimeout2;
                                       //Trace.WriteLine("Added timeout for " + td.Time);
                                   }
                               }
                               finishedAdding2 = true;
                               Trace.WriteLine("*** Finished adding via a second client connection ***");
                           }).Start();

                // Mimic the behavior of the TimeoutPersister coordinator
                var found = 0;
                var startSlice = DateTime.UtcNow.AddYears(-10);
                while (!finishedAdding1 || !finishedAdding2 || startSlice < lastTimeout1 || startSlice < lastTimeout2)
                {
                    DateTime nextRetrieval;
                    var timeoutDatas = persister.GetNextChunk(startSlice, out nextRetrieval);
                    Trace.WriteLine("Querying for timeouts starting at " + startSlice + " with last known added timeouts at " + lastTimeout1 + " & " + lastTimeout2);
                    foreach (var timeoutData in timeoutDatas)
                    {
                        if (startSlice < timeoutData.Item2)
                        {
                            startSlice = timeoutData.Item2;
                        }
                        found++;

                        TimeoutData tmptd;
                        Assert.IsTrue(persister.TryRemove(timeoutData.Item1, out tmptd));
                    }
                }

                var lastTimeout = lastTimeout1 > lastTimeout2 ? lastTimeout1 : lastTimeout2;
                var leftovers = persister.DoCleanup(lastTimeout.AddSeconds(5));
                foreach (var leftover in leftovers)
                {
                    TimeoutData tmptd;
                    persister.TryRemove(leftover.Item1, out tmptd);
                }
                Assert.AreEqual(expected.Count, found + leftovers.Count());

                WaitForIndexing(documentStore);

                using (var session = documentStore.OpenSession())
                {
                    var results = session.Query<Timeout, TimeoutsIndex>().ToList();
                    Assert.AreEqual(0, results.Count);
                }
            }
        }

        static void WaitForIndexing(IDocumentStore store, string db = null, TimeSpan? timeout = null)
        {
            var databaseCommands = store.DatabaseCommands;
            if (db != null)
                databaseCommands = databaseCommands.ForDatabase(db);
            var spinUntil = SpinWait.SpinUntil(() => databaseCommands.GetStatistics().StaleIndexes.Length == 0, timeout ?? TimeSpan.FromSeconds(20));
            Assert.True(spinUntil);
        }

        public static class RandomProvider
        {
            private static int seed = Environment.TickCount;

            private static ThreadLocal<Random> randomWrapper = new ThreadLocal<Random>(() =>
                new Random(Interlocked.Increment(ref seed))
            );

            public static Random GetThreadRandom()
            {
                return randomWrapper.Value;
            }
        }
    }
}
