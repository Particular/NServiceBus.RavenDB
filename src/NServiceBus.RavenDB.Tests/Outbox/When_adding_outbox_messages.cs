namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Outbox;
    using NServiceBus.RavenDB.Outbox;
    using NUnit.Framework;
    using Raven.Abstractions.Exceptions;
    using Raven.Client.Exceptions;

    [TestFixture]
    public class When_adding_outbox_messages : RavenTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            new OutboxRecordsIndex().Execute(store);
        }

        [Test]
        public void Should_throw_if__trying_to_insert_same_messageid()
        {
            var sessionFactory = new RavenSessionFactory(store);
            var persister = new OutboxPersister(sessionFactory);

            using (sessionFactory.Session)
            {
                persister.Store("MySpecialId", Enumerable.Empty<TransportOperation>());
                Assert.Throws<NonUniqueObjectException>(() => persister.Store("MySpecialId", Enumerable.Empty<TransportOperation>()));

                sessionFactory.SaveChanges();
            }
        }

        [Test]
        public void Should_throw_if__trying_to_insert_same_messageid2()
        {
            var sessionFactory = new RavenSessionFactory(store);
            var persister = new OutboxPersister(sessionFactory);

            persister.Store("MySpecialId", Enumerable.Empty<TransportOperation>());
            sessionFactory.SaveChanges();
            sessionFactory.ReleaseSession();

            persister.Store("MySpecialId", Enumerable.Empty<TransportOperation>());
            Assert.Throws<ConcurrencyException>(sessionFactory.SaveChanges);
        }

        [Test]
        public void Should_save_with_not_dispatched()
        {
            var id = Guid.NewGuid().ToString("N");
            var sessionFactory = new RavenSessionFactory(store);

            var persister = new OutboxPersister(sessionFactory){DocumentStore = store};
            persister.Store(id, new List<TransportOperation>
            {
                new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
            });

            sessionFactory.SaveChanges();
            sessionFactory.ReleaseSession();

            OutboxMessage result;
            persister.TryGet(id, out result);

            var operation = result.TransportOperations.Single();

            Assert.AreEqual(id, operation.MessageId);
        }

        [Test]
        public void Should_update_dispatched_flag()
        {
            var id = Guid.NewGuid().ToString("N");

            var sessionFactory = new RavenSessionFactory(store);
            var persister = new OutboxPersister(sessionFactory) { DocumentStore = store };
            persister.Store(id, new List<TransportOperation>
            {
                new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
            });

            sessionFactory.SaveChanges();
            sessionFactory.ReleaseSession();

            persister.SetAsDispatched(id);

            WaitForIndexing(store);

            using (var session = store.OpenSession())
            {
                var result = session.Query<OutboxRecord>().Where(o => o.MessageId == id)
                    .SingleOrDefault();

                Assert.NotNull(result);
                Assert.True(result.Dispatched);
            }
        }
    }
}