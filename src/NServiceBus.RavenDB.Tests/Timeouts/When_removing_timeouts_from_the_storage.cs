namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Transactions;
    using RavenDB.Persistence;
    using RavenDB.Persistence.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Embedded;

    [TestFixture]
    public class When_removing_timeouts_from_the_storage
    {
        IDocumentStore store;
        RavenTimeoutPersistence persister;

        [SetUp]
        public void Setup()
        {
            Address.InitializeLocalAddress("MyEndpoint");

            Configure.GetEndpointNameAction = () => "MyEndpoint";

            store = new EmbeddableDocumentStore {RunInMemory = true};
            //store = new DocumentStore { Url = "http://localhost:8081", DefaultDatabase = "TempTest" };
            store.Conventions.DefaultQueryingConsistency = ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite;
            store.Conventions.MaxNumberOfRequestsPerSession = 10;
            store.Initialize();

            persister = new RavenTimeoutPersistence(new StoreAccessor(store));
        }

        [TearDown]
        public void Cleanup()
        {
            store.Dispose();
        }

        [Test]
        public void TryRemove_should_remove_timeouts_by_id()
        {
            var t1 = new TimeoutData { Id = "1", Time = DateTime.UtcNow.AddHours(-1) };
            persister.Add(t1);

            var t2 = new TimeoutData { Id = "2", Time = DateTime.UtcNow.AddHours(-1) };
            persister.Add(t2);

            var timeouts = GetNextChunk();

            foreach (var timeout in timeouts)
            {
                TimeoutData timeoutData;
                persister.TryRemove(timeout.Item1, out timeoutData);
            }

            Assert.AreEqual(0, GetNextChunk().Count);
        }

        [Test]
        public void TryRemove_should_work_with_concurrent_operations()
        {
            var timeoutData = new TimeoutData { Id = "1", Time = DateTime.UtcNow.AddHours(-1) };
            persister.Add(timeoutData);

            AutoResetEvent t1EnteredTx = new AutoResetEvent(false);
            AutoResetEvent t2EnteredTx = new AutoResetEvent(false);

            bool? t1Remove = null;
            bool? t2Remove = null;
            var t1 = new Thread(() =>
            {
                using (var tx = new TransactionScope())
                {
                    t1EnteredTx.Set();
                    t2EnteredTx.WaitOne();

                    t1Remove = persister.TryRemove(timeoutData.Id);
                    tx.Complete();
                }
            });

            var t2 = new Thread(() =>
            {
                using (var tx = new TransactionScope())
                {
                    t2EnteredTx.Set();
                    t1EnteredTx.WaitOne();

                    t2Remove = persister.TryRemove(timeoutData.Id);
                    tx.Complete();
                }
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            Assert.IsTrue(t1Remove.Value || t2Remove.Value);
            Assert.IsFalse(t1Remove.Value && t2Remove.Value);
        }

        List<Tuple<string, DateTime>> GetNextChunk()
        {
            DateTime nextTimeToRunQuery;
            return persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);
        }
    }
}
