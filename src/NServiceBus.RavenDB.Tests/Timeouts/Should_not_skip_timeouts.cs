using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System.Diagnostics;
    using System.Threading;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Embedded;
    using Timeout.Core;
    using TimeoutPersisters.RavenDB;
    using Timeout = TimeoutPersisters.RavenDB.Timeout;

    [TestFixture]
    public class Should_not_skip_timeouts
    {
        [TestCase]
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
                var startSlice = DateTime.UtcNow.AddYears(-10);
                while (!finishedAdding || startSlice < lastTimeout)
                {
                    DateTime nextRetrieval;

                    var timeoutDatas = persister.GetNextChunk(startSlice, out nextRetrieval);
                    Trace.WriteLine("Querying for timeouts starting at " + startSlice + " with last known added timeout at " + lastTimeout);
                    foreach (var timeoutData in timeoutDatas)
                    {
                        if (startSlice < timeoutData.Item2)
                        {
                            startSlice = timeoutData.Item2;
                        }
                        found++;

                        TimeoutData td;
                        Assert.IsTrue(persister.TryRemove(timeoutData.Item1, out td));
                    }
                }

                Assert.AreEqual(expected.Count, found);

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
