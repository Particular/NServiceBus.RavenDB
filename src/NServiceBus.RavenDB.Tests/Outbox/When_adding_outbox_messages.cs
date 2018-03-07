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
            var sessionFactory = new RavenSessionFactory(store);
            var persister = new OutboxPersister(sessionFactory, CreateTestSessionOpener()) { EndpointName = "TestEndpoint" };
            persister.EndpointName = "TestEndpoint";

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
            var persister = new OutboxPersister(sessionFactory, CreateTestSessionOpener()) { EndpointName = "TestEndpoint" };

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

            var persister = new OutboxPersister(sessionFactory, CreateTestSessionOpener()) { DocumentStore = store, EndpointName = "TestEndpoint" };
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
            var persister = new OutboxPersister(sessionFactory, CreateTestSessionOpener()) { DocumentStore = store, EndpointName = "TestEndpoint" };
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

        [TestCase("Outbox/")]
        [TestCase("Outbox/TestEndpoint/")]
        public void Should_get_messages_with_old_and_new_recordId_format(string outboxRecordIdPrefix)
        {
            var sessionFactory = new RavenSessionFactory(store);
            var persister = new OutboxPersister(sessionFactory, CreateTestSessionOpener()) { DocumentStore = store, EndpointName = "TestEndpoint" };

            var messageId = Guid.NewGuid().ToString();

            //manually store an OutboxRecord to control the OutboxRecordId format
            sessionFactory.Session.Store(new OutboxRecord
            {
                MessageId = messageId,
                Dispatched = false,
                TransportOperations = new List<OutboxRecord.OutboxOperation>
                {
                    new OutboxRecord.OutboxOperation
                    {
                        Message = new byte[1024 * 5],
                        Headers = new Dictionary<string, string>(),
                        MessageId = messageId,
                        Options = new Dictionary<string, string>()
                    }
                }
            }, outboxRecordIdPrefix + messageId);

            sessionFactory.SaveChanges();
            sessionFactory.ReleaseSession();

            OutboxMessage result;
            persister.TryGet(messageId, out result);

            Assert.NotNull(result);
            Assert.AreEqual(messageId, result.MessageId);
        }

        [TestCase("Outbox/")]
        [TestCase("Outbox/TestEndpoint/")]
        public void Should_set_messages_as_dispatched_with_old_and_new_recordId_format(string outboxRecordIdPrefix)
        {
            var sessionFactory = new RavenSessionFactory(store);
            var persister = new OutboxPersister(sessionFactory, CreateTestSessionOpener()) { DocumentStore = store, EndpointName = "TestEndpoint" };

            var messageId = Guid.NewGuid().ToString();

            //manually store an OutboxRecord to control the OutboxRecordId format
            sessionFactory.Session.Store(new OutboxRecord
            {
                MessageId = messageId,
                Dispatched = false,
                TransportOperations = new List<OutboxRecord.OutboxOperation>
                {
                    new OutboxRecord.OutboxOperation
                    {
                        Message = new byte[1024 * 5],
                        Headers = new Dictionary<string, string>(),
                        MessageId = messageId,
                        Options = new Dictionary<string, string>()
                    }
                }
            }, outboxRecordIdPrefix + messageId);

            sessionFactory.SaveChanges();
            sessionFactory.ReleaseSession();

            persister.SetAsDispatched(messageId);
            sessionFactory.ReleaseSession();

            var result = sessionFactory.Session.Load<OutboxRecord>(outboxRecordIdPrefix + messageId);

            Assert.NotNull(result);
            Assert.True(result.Dispatched);
        }

        [Test]
        public void Should_filter_invalid_docid_character()
        {
            var sessionFactory = new RavenSessionFactory(store);
            var persister = new OutboxPersister(sessionFactory, CreateTestSessionOpener()) { DocumentStore = store, EndpointName = "TestEndpoint" };

            var guid = Guid.NewGuid();
            var messageId = $@"{guid}\12345";
            var emptyDictionary = new Dictionary<string, string>();

            using (sessionFactory.Session)
            {
                persister.Store(messageId, new [] { new TransportOperation("test", emptyDictionary, new byte[0], emptyDictionary) });

                sessionFactory.SaveChanges();
            }

            OutboxMessage outboxMessage;
            var result = persister.TryGet(messageId, out outboxMessage);

            Assert.IsTrue(result);
            Assert.AreEqual(messageId, outboxMessage.MessageId);
            Assert.AreEqual(1, outboxMessage.TransportOperations.Count);
            Assert.AreEqual("test", outboxMessage.TransportOperations[0].MessageId);
        }


    }
}
