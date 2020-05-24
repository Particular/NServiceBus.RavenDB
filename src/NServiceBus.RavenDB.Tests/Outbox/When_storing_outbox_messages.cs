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
    public class When_storing_outbox_messages : RavenDBPersistenceTestBase
    {
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
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener());
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
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener());
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
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener());
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outgoingMessageId = "outgoingMessageId";
            var outboxMessage = new OutboxMessage(incomingMessageId, new[] { new TransportOperation(outgoingMessageId, default, default, default) });

            // act
            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(outboxMessage, transaction, context);
                await transaction.Commit();
            }

            // assert
            var storedOutboxMessage = await persister.Get(incomingMessageId, context);
            var storedOutgoingMessage = storedOutboxMessage.TransportOperations.Single();

            Assert.AreEqual(outgoingMessageId, storedOutgoingMessage.MessageId);
        }

        [Test]
        public async Task Should_save_schema_version()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener());
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
        public async Task Should_filter_invalid_docid_character()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener());
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
