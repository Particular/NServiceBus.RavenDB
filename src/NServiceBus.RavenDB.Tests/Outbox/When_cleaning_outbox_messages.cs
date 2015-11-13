namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using NServiceBus.Outbox;
    using NServiceBus.RavenDB.Outbox;
    using NUnit.Framework;

    [TestFixture]
    public class When_cleaning_outbox_messages : RavenTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            new OutboxRecordsIndex().Execute(store);
        }

        [Test]
        public void Should_delete_all_OutboxRecords_that_have_been_dispatched()
        {
            var id = Guid.NewGuid().ToString("N");

            var sessionFactory = new RavenSessionFactory(store);

            var persister = new OutboxPersister(sessionFactory) { DocumentStore = store, EndpointName = "TestEndpoint" };
            persister.Store("NotDispatched", Enumerable.Empty<TransportOperation>());
            persister.Store(id, new List<TransportOperation>
            {
                new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
            });

            sessionFactory.SaveChanges();
            sessionFactory.ReleaseSession();

            persister.SetAsDispatched(id);
            Thread.Sleep(TimeSpan.FromSeconds(1)); //Need to wait for dispatch logic to finish

            WaitForIndexing(store);

            var cleaner = new OutboxRecordsCleaner { DocumentStore = store };
            cleaner.RemoveEntriesOlderThan(DateTime.UtcNow.AddMinutes(1));

            using (var session = store.OpenSession())
            {
                var result = session.Query<OutboxRecord>().ToList();

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("NotDispatched", result[0].MessageId);
            }
        }
    }
}
