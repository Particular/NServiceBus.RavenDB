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

    [TestFixture]
    public class When_cleaning_outbox_messages : RavenDBPersistenceTestBase
    {
        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            await new OutboxRecordsIndex().ExecuteAsync(store);
        }

        [Test]
        public async Task Should_delete_all_dispatched_outbox_records()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default, UseClusterWideTransactions);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var dispatchedOutboxMessage = new OutboxMessage(incomingMessageId, new TransportOperation[0]);
            var notDispatchedOutgoingMessage = new OutboxMessage("NotDispatched", new TransportOperation[0]);

            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(dispatchedOutboxMessage, transaction, context);
                await persister.Store(notDispatchedOutgoingMessage, transaction, context);
                await transaction.Commit();
            }

            await persister.SetAsDispatched(dispatchedOutboxMessage.MessageId, context);
            await Task.Delay(TimeSpan.FromSeconds(1)); // wait for dispatch logic to finish

            await WaitForIndexing();

            var cleaner = new OutboxRecordsCleaner(store);

            // act
            await cleaner.RemoveEntriesOlderThan(DateTime.UtcNow.AddMinutes(1));

            // assert
            using (var session = store.OpenAsyncSession(GetSessionOptions()))
            {
                var outboxRecords = await session.Query<OutboxRecord>().ToListAsync();

                Assert.That(outboxRecords.Count, Is.EqualTo(1));
                Assert.That(outboxRecords.Single().MessageId, Is.EqualTo(notDispatchedOutgoingMessage.MessageId));
            }
        }
    }
}
