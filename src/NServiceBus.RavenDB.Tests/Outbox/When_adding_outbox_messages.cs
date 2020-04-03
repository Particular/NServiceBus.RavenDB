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
    using Raven.Client.Documents;
    using Raven.Client.Exceptions;
    using Raven.Client.Exceptions.Documents.Session;

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
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessage = SimulateIncomingMessage(context);

            var exception = await Catch<NonUniqueObjectException>(async () =>
            {
                using (var transaction = await persister.BeginTransaction(context))
                {
                    await persister.Store(new OutboxMessage(incomingMessage.MessageId, new TransportOperation[0]), transaction, context);
                    await persister.Store(new OutboxMessage(incomingMessage.MessageId, new TransportOperation[0]), transaction, context);
                    await transaction.Commit();
                }
            });

            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_throw_if__trying_to_insert_same_messageid()
        {
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessage = SimulateIncomingMessage(context);

            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(new OutboxMessage(incomingMessage.MessageId, new TransportOperation[0]), transaction, context);

                await transaction.Commit();
            }

            var exception = await Catch<ConcurrencyException>(async () =>
            {
                using (var transaction = await persister.BeginTransaction(context))
                {
                    await persister.Store(new OutboxMessage(incomingMessage.MessageId, new TransportOperation[0]), transaction, context);

                    await transaction.Commit();
                }
            });

            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_save_with_not_dispatched()
        {
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());

            var context = new ContextBag();
            var incomingMessage = SimulateIncomingMessage(context);

            var message = new OutboxMessage(incomingMessage.MessageId, new[]
            {
                new TransportOperation(incomingMessage.MessageId, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>())
            });

            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(message, transaction, context);

                await transaction.Commit();
            }

            var result = await persister.Get(incomingMessage.MessageId, context);

            var operation = result.TransportOperations.Single();

            Assert.AreEqual(incomingMessage.MessageId, operation.MessageId);
        }

        [Test]
        public async Task Should_update_dispatched_flag()
        {
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessage = SimulateIncomingMessage(context);

            var message = new OutboxMessage(incomingMessage.MessageId, new[]
            {
                new TransportOperation(incomingMessage.MessageId, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>())
            });

            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(message, transaction, context);

                await transaction.Commit();
            }
            await persister.SetAsDispatched(incomingMessage.MessageId, context);

            WaitForIndexing();

            using (var s = store.OpenAsyncSession())
            {
                var result = await s.Query<OutboxRecord>()
                    .SingleOrDefaultAsync(o => o.MessageId == incomingMessage.MessageId);

                Assert.NotNull(result);
                Assert.True(result.Dispatched);
            }
        }

        [Test]
        public async Task Should_get_messages()
        {
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessage = SimulateIncomingMessage(context);

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = OpenAsyncSession())
            {
                var newRecord = new OutboxRecord
                {
                    MessageId = incomingMessage.MessageId,
                    Dispatched = false,
                    TransportOperations = new[]
                    {
                        new OutboxRecord.OutboxOperation
                        {
                            Message = new byte[1024*5],
                            Headers = new Dictionary<string, string>(),
                            MessageId = incomingMessage.MessageId,
                            Options = new Dictionary<string, string>()
                        }
                    }
                };
                var fullDocumentId = "Outbox/TestEndpoint/" + incomingMessage.MessageId;
                await session.StoreAsync(newRecord, fullDocumentId);

                await session.SaveChangesAsync();
            }

            var result = await persister.Get(incomingMessage.MessageId, context);

            Assert.NotNull(result);
            Assert.AreEqual(incomingMessage.MessageId, result.MessageId);
        }

        [Test]
        public async Task Should_set_messages_as_dispatched()
        {
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessage = SimulateIncomingMessage(context);

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = OpenAsyncSession())
            {
                await session.StoreAsync(new OutboxRecord
                {
                    MessageId = incomingMessage.MessageId,
                    Dispatched = false,
                    TransportOperations = new[]
                    {
                        new OutboxRecord.OutboxOperation
                        {
                            Message = new byte[1024*5],
                            Headers = new Dictionary<string, string>(),
                            MessageId = incomingMessage.MessageId,
                            Options = new Dictionary<string, string>()
                        }
                    }
                }, "Outbox/TestEndpoint/" + incomingMessage.MessageId);

                await session.SaveChangesAsync();
            }

            await persister.SetAsDispatched(incomingMessage.MessageId, context);

            using (var session = OpenAsyncSession())
            {
                var result = await session.LoadAsync<OutboxRecord>("Outbox/TestEndpoint/" + incomingMessage.MessageId);

                Assert.NotNull(result);
                Assert.True(result.Dispatched);
            }
        }

        [Test]
        public async Task Should_filter_invalid_docid_character()
        {
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());

            var guid = Guid.NewGuid();
            var messageId = $@"{guid}\12345";

            var context = new ContextBag();

            SimulateIncomingMessage(context, messageId);

            using (var transaction = await persister.BeginTransaction(context))
            {
                var transportOperations = new[] {
                    new TransportOperation("test", new Dictionary<string, string>(), new byte[0], new Dictionary<string, string>())
                };

                await persister.Store(new OutboxMessage(messageId, transportOperations), transaction, context);
                await transaction.Commit();
            }

            var outboxMessage = await persister.Get(messageId, context);

            Assert.AreEqual(messageId, outboxMessage.MessageId);
            Assert.AreEqual(1, outboxMessage.TransportOperations.Length);
            Assert.AreEqual("test", outboxMessage.TransportOperations[0].MessageId);
        }

    }
}