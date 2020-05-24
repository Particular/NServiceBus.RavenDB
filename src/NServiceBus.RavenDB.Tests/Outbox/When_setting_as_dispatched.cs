namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Outbox;
    using NUnit.Framework;
    using Raven.Client.Documents;

    [TestFixture]
    public class When_setting_as_dispatched : RavenDBPersistenceTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            new OutboxRecordsIndex().Execute(store);
        }

        [Test]
        public async Task Should_update_dispatched_flag()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener());
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
        public async Task Should_set_messages_as_dispatched()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener());
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
    }
}
