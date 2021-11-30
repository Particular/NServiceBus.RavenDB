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
        public override async Task SetUp()
        {
            await base.SetUp();
            await new OutboxRecordsIndex().ExecuteAsync(store);
        }

        [Test]
        public async Task Should_throw_if_trying_to_insert_two_messages_with_the_same_id_in_the_same_transaction()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default, UseClusterWideTransactions);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxMessage1 = new OutboxMessage(incomingMessageId, new TransportOperation[0]);
            var outboxMessage2 = new OutboxMessage(incomingMessageId, new TransportOperation[0]);

            // act
            var exception = await Catch<NonUniqueObjectException>(async cancellationToken =>
            {
                using (var transaction = await persister.BeginTransaction(context, cancellationToken))
                {
                    await persister.Store(outboxMessage1, transaction, context, cancellationToken);
                    await persister.Store(outboxMessage2, transaction, context, cancellationToken);
                    await transaction.Commit(cancellationToken);
                }
            });

            // asssert
            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_throw_if_trying_to_insert_two_messages_with_the_same_id()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default, UseClusterWideTransactions);
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
            var exception = await Catch<ConcurrencyException>(async cancellationToken =>
            {
                using (var transaction = await persister.BeginTransaction(context, cancellationToken))
                {
                    await persister.Store(outboxMessage2, transaction, context, cancellationToken);
                    await transaction.Commit(cancellationToken);
                }
            });

            // assert
            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_store_outbox_record_as_not_dispatched()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default, UseClusterWideTransactions);
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
            using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                var outboxRecord = await session.Query<OutboxRecord>().SingleAsync(record => record.MessageId == incomingMessageId);

                Assert.NotNull(outboxRecord);
                Assert.False(outboxRecord.Dispatched);
                Assert.Null(outboxRecord.DispatchedAt);
                Assert.AreEqual(1, outboxRecord.TransportOperations.Length);
                Assert.AreEqual(outgoingMessageId, outboxRecord.TransportOperations.Single().MessageId);
            }
        }

        [Test]
        public async Task Should_store_schema_version_in_metadata()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default, UseClusterWideTransactions);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxMessage = new OutboxMessage(incomingMessageId, new[] { new TransportOperation("foo", default, default, default) });

            // act
            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(outboxMessage, transaction, context);
                await transaction.Commit();
            }

            await WaitForIndexing();

            // assert
            using (var session = store.OpenAsyncSession(GetSessionOptions()))
            {
                var outboxRecord = await session.Query<OutboxRecord>().SingleAsync(record => record.MessageId == incomingMessageId);
                var metadata = session.Advanced.GetMetadataFor(outboxRecord);

                Assert.AreEqual(OutboxRecord.SchemaVersion, metadata[SchemaVersionExtensions.OutboxRecordSchemaVersionMetadataKey]);
            }
        }

        [Test]
        public async Task Should_filter_invalid_docid_character()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default, UseClusterWideTransactions);
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
