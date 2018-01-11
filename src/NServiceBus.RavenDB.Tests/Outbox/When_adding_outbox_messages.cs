namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Outbox;
    using NUnit.Framework;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;
    using Raven.Client.Exceptions;

    [TestFixture]
    public class When_adding_outbox_messages : RavenDBPersistenceTestBase
    {
        string testEndpointName = "TestEndpoint";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            new OutboxRecordsIndex().Execute(store);
        }

        [Test]
        public async Task Should_throw_if__trying_to_insert_same_messageid_concurrently()
        {
            var persister = new OutboxPersister(store, testEndpointName, CreateTestSessionOpener());

            var exception = await Catch<NonUniqueObjectException>(async () =>
            {
                using (var transaction = await persister.BeginTransaction(new ContextBag()))
                {
                    await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transaction, new ContextBag());
                    await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transaction, new ContextBag());
                    await transaction.Commit();
                }
            });

            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_throw_if__trying_to_insert_same_messageid()
        {
            var persister = new OutboxPersister(store, testEndpointName, CreateTestSessionOpener());

            using (var transaction = await persister.BeginTransaction(new ContextBag()))
            {
                await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transaction, new ContextBag());

                await transaction.Commit();
            }

            var exception = await Catch<ConcurrencyException>(async () =>
            {
                using (var transaction = await persister.BeginTransaction(new ContextBag()))
                {
                    await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transaction, new ContextBag());

                    await transaction.Commit();
                }
            });
            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_save_with_not_dispatched()
        {
            var persister = new OutboxPersister(store, testEndpointName, CreateTestSessionOpener());

            var id = Guid.NewGuid().ToString("N");
            var message = new OutboxMessage(id, new[]
            {
                new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>())
            });

            using (var transaction = await persister.BeginTransaction(new ContextBag()))
            {
                await persister.Store(message, transaction, new ContextBag());

                await transaction.Commit();
            }

            var result = await persister.Get(id, new ContextBag());

            var operation = result.TransportOperations.Single();

            Assert.AreEqual(id, operation.MessageId);
        }

        [Test]
        public async Task Should_update_dispatched_flag()
        {
            var persister = new OutboxPersister(store, testEndpointName, CreateTestSessionOpener());

            var id = Guid.NewGuid().ToString("N");
            var message = new OutboxMessage(id, new []
            {
                new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>())
            });

            using (var transaction = await persister.BeginTransaction(new ContextBag()))
            {
                await persister.Store(message, transaction, new ContextBag());

                await transaction.Commit();
            }
            await persister.SetAsDispatched(id, new ContextBag());

            WaitForIndexing();

            using (var s = store.OpenAsyncSession())
            {
                var result = await s.Query<OutboxRecord>()
                    .SingleOrDefaultAsync(o => o.MessageId == id);

                Assert.NotNull(result);
                Assert.True(result.Dispatched);
            }
        }

        [TestCase("Outbox/")]
        [TestCase("Outbox/TestEndpoint/")]
        public async Task Should_get_messages_with_old_and_new_recordId_format(string outboxRecordIdPrefix)
        {
            var persister = new OutboxPersister(store, testEndpointName, CreateTestSessionOpener());

            var messageId = Guid.NewGuid().ToString();

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = OpenAsyncSession())
            {
                var newRecord = new OutboxRecord
                {
                    MessageId = messageId,
                    Dispatched = false,
                    TransportOperations = new[]
                    {
                        new OutboxRecord.OutboxOperation
                        {
                            Message = new byte[1024*5],
                            Headers = new Dictionary<string, string>(),
                            MessageId = messageId,
                            Options = new Dictionary<string, string>()
                        }
                    }
                };
                var fullDocumentId = outboxRecordIdPrefix + messageId;
                await session.StoreAsync(newRecord, fullDocumentId);

                await session.SaveChangesAsync();
            }
            
            var result = await persister.Get(messageId, new ContextBag());

            Assert.NotNull(result);
            Assert.AreEqual(messageId, result.MessageId);
        }

        [TestCase("Outbox/")]
        [TestCase("Outbox/TestEndpoint/")]
        public async Task Should_set_messages_as_dispatched_with_old_and_new_recordId_format(string outboxRecordIdPrefix)
        {
            var persister = new OutboxPersister(store, testEndpointName, CreateTestSessionOpener());

            var messageId = Guid.NewGuid().ToString();

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = OpenAsyncSession())
            {
                await session.StoreAsync(new OutboxRecord
                {
                    MessageId = messageId,
                    Dispatched = false,
                    TransportOperations = new []
                    {
                        new OutboxRecord.OutboxOperation
                        {
                            Message = new byte[1024*5],
                            Headers = new Dictionary<string, string>(),
                            MessageId = messageId,
                            Options = new Dictionary<string, string>()
                        }
                    }
                }, outboxRecordIdPrefix + messageId);

                await session.SaveChangesAsync();
            }

            await persister.SetAsDispatched(messageId, new ContextBag());

            using (var session = OpenAsyncSession())
            {
                var result = await session.LoadAsync<OutboxRecord>(outboxRecordIdPrefix + messageId);

                Assert.NotNull(result);
                Assert.True(result.Dispatched);
            }
            
        }

        [Test]
        public async Task Should_filter_invalid_docid_character()
        {
            var persister = new OutboxPersister(store, testEndpointName, CreateTestSessionOpener());

            var guid = Guid.NewGuid();
            var messageId = $@"{guid}\12345";
            var emptyDictionary = new Dictionary<string, string>();
            var operation = new TransportOperation("test", emptyDictionary, new byte[0], emptyDictionary);
            var transportOperations = new [] { operation };

            using (var transaction = await persister.BeginTransaction(new ContextBag()))
            {
                await persister.Store(new OutboxMessage(messageId, transportOperations), transaction, new ContextBag());
                await transaction.Commit();
            }

            var outboxMessage = await persister.Get(messageId, new ContextBag());

            Assert.AreEqual(messageId, outboxMessage.MessageId);
            Assert.AreEqual(1, outboxMessage.TransportOperations.Length);
            Assert.AreEqual("test", outboxMessage.TransportOperations[0].MessageId);
        }

    }
}