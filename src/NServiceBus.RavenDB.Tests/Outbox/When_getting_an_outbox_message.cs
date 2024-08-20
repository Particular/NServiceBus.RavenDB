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

        [Test]
        public async Task Should_get_the_message()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default, UseClusterWideTransactions);
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
            using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
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
            Assert.That(outboxMessage, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(outboxMessage.MessageId, Is.EqualTo(incomingMessageId));
                Assert.That(outboxMessage.TransportOperations.Length, Is.EqualTo(1));
            });

            var outgoingMessage = outboxMessage.TransportOperations[0];
            Assert.Multiple(() =>
            {
                Assert.That(outgoingMessage.MessageId, Is.EqualTo(outboxOperation.MessageId));
                Assert.That(outgoingMessage.Headers, Is.EqualTo(outboxOperation.Headers));
                Assert.That(outgoingMessage.Body.ToArray(), Is.EqualTo(outboxOperation.Message));
                Assert.That(outgoingMessage.Options, Is.EqualTo(outboxOperation.Options));
            });
        }
    }
}
