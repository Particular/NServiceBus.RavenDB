namespace NServiceBus.RavenDB.Tests.Persistence.DocumentIds
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.TimeoutPersisters.RavenDB;
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

                // Need to ensure multiple runs will work, after conventions document is stored
                for (var i = 0; i < 3; i++)
                {
                    using (var store = db.NewStore())
                    {
                        Console.WriteLine($"Testing receives with DocumentStore initially configured for {seedType} conventions.");
                        ApplyTestConventions(store, seedType);
                        store.Initialize();

                        var index = CreateTimeoutIndex(store);
                        db.WaitForIndexing(store);

                        Assert.AreEqual(index, prefillIndex, "Index definitions must match or previous timeouts will not be found.");

                        var persister = new TimeoutPersister
                        {
                            DocumentStore = store,
                            EndpointName = EndpointName
                        };

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

                    var exception = Assert.Throws<InvalidOperationException>(() => store.Conventions.FindTypeTagName(typeof(TimeoutPersisters.RavenDB.TimeoutData)));
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

                    var persister = new TimeoutPersister
                    {
                        DocumentStore = store,
                        EndpointName = EndpointName
                    };

                    for (var i = 1; i <= 10; i++)
                    {
                        persister.Add(new TimeoutData
                        {
                            Destination = new Address(EndpointName, "localhost"),
                            Headers = new Dictionary<string, string>(),
                            OwningTimeoutManager = EndpointName,
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
                Destination = new Address(EndpointName, "localhost"),
                Headers = new Dictionary<string, string>(),
                OwningTimeoutManager = EndpointName,
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
            Console.WriteLine("Creating index TimeoutsIndex:");

            var timeoutIndex = new TimeoutsIndex();
            timeoutIndex.Execute(store);

            var indexDef = timeoutIndex.CreateIndexDefinition().ToString();
            Console.WriteLine(indexDef);

            return indexDef;
        }
    }
}