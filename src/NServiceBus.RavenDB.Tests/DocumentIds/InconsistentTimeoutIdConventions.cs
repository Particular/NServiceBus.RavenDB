namespace NServiceBus.RavenDB.Tests.DocumentIds
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.RavenDB.Persistence.TimeoutPersister;
    using NUnit.Framework;
    using Raven.Client;
    using TimeoutData = NServiceBus.Timeout.Core.TimeoutData;

    [TestFixture]
    public class InconsistentTimeoutIdConventions : DocumentIdConventionTestBase
    {
        readonly DateTime dueTimeout = new DateTime(DateTime.UtcNow.Year, 1, 1);

        [TestCase(ConventionType.RavenDefault)]
        [TestCase(ConventionType.NSBDefault)]
        [TestCase(ConventionType.Customer)]
        public void TestReceivingTimeouts(ConventionType seedType)
        {
            using (var db = new ReusableDB())
            {
                string prefillIndex;

                using (var store = db.NewStore())
                {
                    ApplyPrefillConventions(store, seedType);
                    store.Initialize();

                    Prefill(store, seedType);

                    prefillIndex = CreateTimeoutIndex(store);
                }

                using (var store = db.NewStore())
                {
                    Console.WriteLine($"Testing receives with DocumentStore initially configured for {seedType} conventions.");
                    ApplyTestConventions(store, seedType);
                    store.Initialize();

                    var index = CreateTimeoutIndex(store);
                    db.WaitForIndexing(store);

                    Assert.AreEqual(index, prefillIndex, "Index definitions must match or previous timeouts will not be found.");

                    var storeAccessor = new StoreAccessor(store);
                    var persister = new RavenTimeoutPersistence(storeAccessor);

                    DateTime nextTimeToRunQuery;
                    var chunkTuples = persister.GetNextChunk(DateTime.UtcNow.AddYears(-10), out nextTimeToRunQuery).ToArray();

                    Assert.AreEqual(10, chunkTuples.Length);
                    foreach (var tuple in chunkTuples)
                    {
                        Console.WriteLine($"Received timeout {tuple.Item1}");
                        Assert.AreEqual(dueTimeout, tuple.Item2);
                    }
                }
            }
        }

        [Test]
        public void EnsureMultipleExistingStrategiesWillThrow()
        {
            using (var db = new ReusableDB())
            {
                using (var store = db.NewStore())
                {
                    ApplyPrefillConventions(store, ConventionType.RavenDefault);
                    store.Initialize();

                    Prefill(store, ConventionType.RavenDefault);
                }

                using (var store = db.NewStore())
                {
                    ApplyPrefillConventions(store, ConventionType.NSBDefault);
                    store.Initialize();

                    Prefill(store, ConventionType.NSBDefault);
                }

                using (var store = db.NewStore())
                {
                    ApplyTestConventions(store, ConventionType.RavenDefault);
                    store.Initialize();

                    var exception = Assert.Throws<InvalidOperationException>(() => store.Conventions.FindTypeTagName(typeof(TimeoutData)));
                    Console.WriteLine($"Got expected exception: {exception.Message}");
                }
            }
        }

        [TestCase(ConventionType.RavenDefault)]
        [TestCase(ConventionType.NSBDefault)]
        [TestCase(ConventionType.Customer)]
        public void EnsureOldAndNewTimeoutsCanBeReceived(ConventionType seedType)
        {
            using (var db = new ReusableDB())
            {
                using (var store = db.NewStore())
                {
                    ApplyPrefillConventions(store, seedType);
                    store.Initialize();

                    Prefill(store, seedType);
                }

                using (var store = db.NewStore())
                {
                    ApplyTestConventions(store, seedType);
                    store.Initialize();

                    CreateTimeoutIndex(store);

                    var storeAccessor = new StoreAccessor(store);
                    var persister = new RavenTimeoutPersistence(storeAccessor);

                    for (var i = 1; i <= 10; i++)
                    {
                        persister.Add(new TimeoutData
                        {
                            Destination = new Address(Configure.EndpointName, "localhost"),
                            Headers = new Dictionary<string, string>(),
                            OwningTimeoutManager = Configure.EndpointName,
                            SagaId = Guid.NewGuid(),
                            Time = dueTimeout
                        });
                    }

                    db.WaitForIndexing(store);

                    DateTime nextTimeToRunQuery;
                    var chunkTuples = persister.GetNextChunk(DateTime.UtcNow.AddYears(-10), out nextTimeToRunQuery).ToArray();

                    Assert.AreEqual(20, chunkTuples.Length);
                    foreach (var tuple in chunkTuples)
                    {
                        Console.WriteLine($"Received timeout {tuple.Item1}");
                        Assert.AreEqual(dueTimeout, tuple.Item2);
                    }
                }
            }
        }

        private void Prefill(IDocumentStore store, ConventionType seedType)
        {
            Console.WriteLine($"Filling database with timeout data, using {seedType} conventions");
            var timeout = new TimeoutData
            {
                Destination = new Address(Configure.EndpointName, "localhost"),
                Headers = new Dictionary<string, string>(),
                OwningTimeoutManager = Configure.EndpointName,
                Time = new DateTime(DateTime.UtcNow.Year, 1, 1)
            };

            var names = new Dictionary<ConventionType, string>
            {
                { ConventionType.RavenDefault, "TimeoutDatas" },
                { ConventionType.NSBDefault, "TimeoutData" },
                { ConventionType.Customer, "ataDtuoemiT" }
            };
            var name = names[seedType];

            for (var i = 1; i <= 10; i++)
            {
                DirectStore(store, $"{name}/{i}", timeout, name);
            }

            StoreHiLo(store, name, 32);
        }

        private string CreateTimeoutIndex(IDocumentStore store)
        {
            RavenQueryStatistics stats;

            using (var session = store.OpenSession())
            {
                var results = session.Query<TimeoutData>()
                    .Customize(c => c.WaitForNonStaleResultsAsOfNow())
                    .Statistics(out stats)
                    .Where(to => to.OwningTimeoutManager == Configure.EndpointName)
                    .Take(1)
                    .ToArray();

                Console.WriteLine("Ensured temp index for timeouts using fake query. {0}", results.Length > 0 ? "First item is " + results[0].Id : "Index contains no results.");
            }

            var index = store.DatabaseCommands.GetIndex(stats.IndexName);
            return index.Map;
        }
    }
}