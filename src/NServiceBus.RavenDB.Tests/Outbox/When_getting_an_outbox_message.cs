namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Outbox;
    using NUnit.Framework;

    [TestFixture]
    public class When_getting_an_outbox_message : RavenDBPersistenceTestBase
    {
        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            await new OutboxRecordsIndex().ExecuteAsync(store);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_get_the_message(bool useClusterWideTransactions)
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default, useClusterWideTransactions);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxOperation = new OutboxRecord.OutboxOperation
            {
                MessageId = "outgoingMessageId",
                Headers = new Dictionary<string, string> { { "headerName1", "headerValue1" } },
                Message = new byte[] { 1, 2, 3 },
                Options = new Dictionary<string, string> { { "optionName1", "optionValue1" } },
            };

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency())
            {
                var outboxRecord = new OutboxRecord
                {
                    MessageId = incomingMessageId,
                    Dispatched = false,
                    TransportOperations = new[] { outboxOperation }
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
            Assert.AreEqual(1, outboxMessage.TransportOperations.Length);

            var outgoingMessage = outboxMessage.TransportOperations[0];
            Assert.AreEqual(outboxOperation.MessageId, outgoingMessage.MessageId);
            Assert.AreEqual(outboxOperation.Headers, outgoingMessage.Headers);
            Assert.AreEqual(outboxOperation.Message, outgoingMessage.Body.ToArray());
            Assert.AreEqual(outboxOperation.Options, outgoingMessage.Options);
        }
    }
}
