namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Outbox;
    using NUnit.Framework;

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
                        TransportOperations = new[] { new OutboxRecord.OutboxOperation { MessageId = "foo", } }
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
