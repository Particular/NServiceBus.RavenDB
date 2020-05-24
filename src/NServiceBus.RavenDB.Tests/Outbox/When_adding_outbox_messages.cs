namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System;
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
        public async Task Should_throw_if_trying_to_insert_same_messageid_concurrently()
        {
            // arrange
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxMessage1 = new OutboxMessage(incomingMessageId, new TransportOperation[0]);
            var outboxMessage2 = new OutboxMessage(incomingMessageId, new TransportOperation[0]);

            // act
            var exception = await Catch<NonUniqueObjectException>(async () =>
            {
                using (var transaction = await persister.BeginTransaction(context))
                {
                    await persister.Store(outboxMessage1, transaction, context);
                    await persister.Store(outboxMessage2, transaction, context);
                    await transaction.Commit();
                }
            });

            // asssert
            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_throw_if_trying_to_insert_same_messageid()
        {
            // arrange
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxMessage1 = new OutboxMessage(incomingMessageId, new TransportOperation[0]);
            var outboxMessage2 = new OutboxMessage(incomingMessageId, new TransportOperation[0]);

            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(outboxMessage1, transaction, context);
                await transaction.Commit();
            }

            // act
            var exception = await Catch<ConcurrencyException>(async () =>
            {
                using (var transaction = await persister.BeginTransaction(context))
                {
                    await persister.Store(outboxMessage2, transaction, context);
                    await transaction.Commit();
                }
            });

            // assert
            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_save_with_not_dispatched()
        {
            // arrange
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxMessage = new OutboxMessage(incomingMessageId, new[] { new TransportOperation(incomingMessageId, default, default, default) });

            // act
            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(outboxMessage, transaction, context);
                await transaction.Commit();
            }

            // assert
            var storedOutboxMessage = await persister.Get(incomingMessageId, context);
            var storedOutgoingMessage = storedOutboxMessage.TransportOperations.Single();

            Assert.AreEqual(incomingMessageId, storedOutgoingMessage.MessageId);
        }

        [Test]
        public async Task Should_save_schema_version()
        {
            // arrange
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxMessage = new OutboxMessage(incomingMessageId, new[] { new TransportOperation("foo", default, default, default) });

            // act
            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(outboxMessage, transaction, context);
                await transaction.Commit();
            }

            WaitForIndexing();

            // assert
            using (var session = store.OpenAsyncSession())
            {
                var outboxRecord = await session.Query<OutboxRecord>().SingleOrDefaultAsync(record => record.MessageId == incomingMessageId);
                var metadata = session.Advanced.GetMetadataFor(outboxRecord);

                Assert.AreEqual(OutboxRecord.SchemaVersion, metadata[SchemaVersionExtensions.OutboxRecordSchemaVersionMetadataKey]);
            }
        }

        [Test]
        public async Task Should_update_dispatched_flag()
        {
            // arrange
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxMessage = new OutboxMessage(incomingMessageId, new[] { new TransportOperation(incomingMessageId, default, default, default) });

            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(outboxMessage, transaction, context);
                await transaction.Commit();
            }

            // act
            await persister.SetAsDispatched(incomingMessageId, context);
            WaitForIndexing();

            // assert
            using (var session = store.OpenAsyncSession())
            {
                var outboxRecord = await session.Query<OutboxRecord>().SingleOrDefaultAsync(record => record.MessageId == incomingMessageId);

                Assert.NotNull(outboxRecord);
                Assert.True(outboxRecord.Dispatched);
            }
        }

        [Test]
        public async Task Should_get_messages()
        {
            // arrange
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = OpenAsyncSession())
            {
                var outboxRecord = new OutboxRecord
                {
                    MessageId = incomingMessageId,
                    Dispatched = false,
                    TransportOperations = new[] { new OutboxRecord.OutboxOperation { MessageId = incomingMessageId, } }
                };

                var outboxRecordId = "Outbox/TestEndpoint/" + incomingMessageId;

                await session.StoreAsync(outboxRecord, outboxRecordId);
                await session.SaveChangesAsync();
            }

            // act
            var outboxMessage = await persister.Get(incomingMessageId, context);

            // assert
            Assert.NotNull(outboxMessage);
            Assert.AreEqual(incomingMessageId, outboxMessage.MessageId);
        }

        [Test]
        public async Task Should_set_messages_as_dispatched()
        {
            // arrange
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = OpenAsyncSession())
            {
                await session.StoreAsync(
                    new OutboxRecord
                    {
                        MessageId = incomingMessageId,
                        Dispatched = false,
                        TransportOperations = new[] { new OutboxRecord.OutboxOperation { MessageId = incomingMessageId, } }
                    },
                    "Outbox/TestEndpoint/" + incomingMessageId);

                await session.SaveChangesAsync();
            }

            // act
            await persister.SetAsDispatched(incomingMessageId, context);

            // assert
            using (var session = OpenAsyncSession())
            {
                var outboxRecord = await session.LoadAsync<OutboxRecord>("Outbox/TestEndpoint/" + incomingMessageId);

                Assert.NotNull(outboxRecord);
                Assert.True(outboxRecord.Dispatched);
            }
        }

        [Test]
        public async Task Should_filter_invalid_docid_character()
        {
            // arrange
            var persister = new OutboxPersister(testEndpointName, CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessageId = $@"{Guid.NewGuid()}\12345";

            SimulateIncomingMessage(context, incomingMessageId);

            var outboxMessage = new OutboxMessage(incomingMessageId, new[] { new TransportOperation("test", default, default, default) });

            // act
            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(outboxMessage, transaction, context);
                await transaction.Commit();
            }

            // assert
            var storedOutboxMessage = await persister.Get(incomingMessageId, context);

            Assert.AreEqual(incomingMessageId, storedOutboxMessage.MessageId);
            Assert.AreEqual(1, storedOutboxMessage.TransportOperations.Length);
            Assert.AreEqual("test", storedOutboxMessage.TransportOperations[0].MessageId);
        }
    }
}
