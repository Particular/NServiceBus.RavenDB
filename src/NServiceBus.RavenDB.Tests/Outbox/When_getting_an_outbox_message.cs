namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Outbox;
    using NUnit.Framework;

    [TestFixture]
    public class When_getting_an_outbox_message : RavenDBPersistenceTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            new OutboxRecordsIndex().Execute(store);
        }

        [Test]
        public async Task Should_get_messages()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener());
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
    }
}
