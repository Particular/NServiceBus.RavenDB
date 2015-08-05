namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Outbox;
    using NServiceBus.RavenDB.Outbox;
    using NUnit.Framework;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;
    using Raven.Client.Exceptions;

    [TestFixture]
    public class When_adding_outbox_messages : RavenDBPersistenceTestBase
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
            IDocumentSession session;
            var options = this.NewOptions(out session);
            var persister = new OutboxPersister();

            using (session)
            {
                persister.Store("MySpecialId", Enumerable.Empty<TransportOperation>(), options);
                Assert.Throws<NonUniqueObjectException>(() => persister.Store("MySpecialId", Enumerable.Empty<TransportOperation>(), options));

                session.SaveChanges();
            }
        }

        [Test]
        public void Should_throw_if__trying_to_insert_same_messageid2()
        {
            IDocumentSession session;
            var options = this.NewOptions(out session);
            var persister = new OutboxPersister();

            persister.Store("MySpecialId", Enumerable.Empty<TransportOperation>(), options);
            session.SaveChanges();
            session.Dispose();

            options = this.NewOptions(out session);
            persister.Store("MySpecialId", Enumerable.Empty<TransportOperation>(), options);
            Assert.Throws<ConcurrencyException>(session.SaveChanges);
        }

        [Test]
        public void Should_save_with_not_dispatched()
        {
            var id = Guid.NewGuid().ToString("N");
            IDocumentSession session;
            var options = this.NewOptions(out session);

            var persister = new OutboxPersister { DocumentStore = store };
            persister.Store(id, new List<TransportOperation>
            {
                new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
            }, options);

            session.SaveChanges();
            session.Dispose();

            options = this.NewOptions(out session);
            OutboxMessage result;
            persister.TryGet(id, options, out result);

            var operation = result.TransportOperations.Single();

            Assert.AreEqual(id, operation.MessageId);
        }

        [Test]
        public void Should_update_dispatched_flag()
        {
            var id = Guid.NewGuid().ToString("N");

            IDocumentSession session;
            var options = this.NewOptions(out session);
            var persister = new OutboxPersister{ DocumentStore = store };
            persister.Store(id, new List<TransportOperation>
            {
                new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
            }, options);

            session.SaveChanges();
            session.Dispose();

            options = this.NewOptions(out session);
            persister.SetAsDispatched(id, options);

            WaitForIndexing(store);

            using (var s = store.OpenSession())
            {
                var result = s.Query<OutboxRecord>()
                    .SingleOrDefault(o => o.MessageId == id);

                Assert.NotNull(result);
                Assert.True(result.Dispatched);
            }
        }
    }
}