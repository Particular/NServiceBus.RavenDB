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
    using Raven.Client.Documents.Operations.Expiration;

    [TestFixture]
    public class When_outbox_messages_expire : RavenDBPersistenceTestBase
    {
        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            await new OutboxRecordsIndex().ExecuteAsync(store);
        }

        [Test]
        public async Task Should_be_deleted()
        {
            await store.Maintenance.SendAsync(
                new ConfigureExpirationOperation(
                    new ExpirationConfiguration { Disabled = false, DeleteFrequencyInSec = 1, }));

            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), TimeSpan.FromSeconds(1), UseClusterWideTransactions);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var dispatchedOutboxMessage = new OutboxMessage(incomingMessageId, new TransportOperation[0]);
            var notDispatchedOutboxMessage = new OutboxMessage("NotDispatched", new TransportOperation[0]);

            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(dispatchedOutboxMessage, transaction, context);
                await persister.Store(notDispatchedOutboxMessage, transaction, context);
                await transaction.Commit();
            }

            await persister.SetAsDispatched(dispatchedOutboxMessage.MessageId, context);

            // act
            // wait for dispatch logic and expiry to finish, not ideal but polling on BASE index is also not great
            await Task.Delay(TimeSpan.FromSeconds(3));
            await WaitForIndexing();

            // assert
            using (var session = store.OpenAsyncSession(GetSessionOptions()))
            {
                var outboxRecords = await session.Query<OutboxRecord>().ToListAsync();

                Assert.That(outboxRecords, Has.Count.EqualTo(1));
                Assert.That(outboxRecords.Single().MessageId, Is.EqualTo(notDispatchedOutboxMessage.MessageId));
            }
        }
    }
}
