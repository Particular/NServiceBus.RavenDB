namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Outbox;
    using NUnit.Framework;
    using Raven.Client;

    [TestFixture]
    public class When_setting_as_dispatched : RavenDBPersistenceTestBase
    {
        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();
            await new OutboxRecordsIndex().ExecuteAsync(store);
        }

        [Test]
        public async Task Should_set_outbox_record_as_dispatched()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default, UseClusterWideTransactions);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxRecordId = "Outbox/TestEndpoint/" + incomingMessageId;

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                await session.StoreAsync(
                    new OutboxRecord
                    {
                        MessageId = incomingMessageId,
                        Dispatched = false,
                        TransportOperations = new[] { new OutboxRecord.OutboxOperation { MessageId = "foo", } }
                    },
                    outboxRecordId);

                await session.SaveChangesAsync();
            }

            // act
            await persister.SetAsDispatched(incomingMessageId, context);

            // assert
            using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                var outboxRecord = await session.LoadAsync<OutboxRecord>(outboxRecordId);

                Assert.That(outboxRecord, Is.Not.Null);
                Assert.Multiple(() =>
                {
                    Assert.That(outboxRecord.Dispatched, Is.True);
                    Assert.That(outboxRecord.DispatchedAt, Is.Not.Null);
                    Assert.That(outboxRecord.TransportOperations, Is.Empty);
                });
            }
        }

        [Test]
        public async Task Should_store_schema_version_in_metadata()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default, UseClusterWideTransactions);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxRecordId = "Outbox/TestEndpoint/" + incomingMessageId;

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                await session.StoreAsync(new OutboxRecord { MessageId = incomingMessageId, Dispatched = false, }, outboxRecordId);
                await session.SaveChangesAsync();
            }

            // act
            await persister.SetAsDispatched(incomingMessageId, context);

            // assert
            using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                var outboxRecord = await session.LoadAsync<OutboxRecord>(outboxRecordId);
                var metadata = session.Advanced.GetMetadataFor(outboxRecord);

                Assert.That(metadata[SchemaVersionExtensions.OutboxRecordSchemaVersionMetadataKey], Is.EqualTo(OutboxRecord.SchemaVersion));
            }
        }

        [Test]
        public async Task Should_store_expiry_in_metadata_if_time_to_keep_deduplication_data_is_finite()
        {
            // arrange
            var timeToKeepDeduplicationData = TimeSpan.FromSeconds(60);
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), timeToKeepDeduplicationData, UseClusterWideTransactions);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxRecordId = "Outbox/TestEndpoint/" + incomingMessageId;

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                await session.StoreAsync(new OutboxRecord { MessageId = incomingMessageId, Dispatched = false, }, outboxRecordId);
                await session.SaveChangesAsync();
            }

            var expectedExpiry = DateTime.UtcNow.Add(timeToKeepDeduplicationData);
            var maxExpectedExecutionTime = TimeSpan.FromMinutes(15);

            // act
            await persister.SetAsDispatched(incomingMessageId, context);

            // assert
            using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                var outboxRecord = await session.LoadAsync<OutboxRecord>(outboxRecordId);
                var metadata = session.Advanced.GetMetadataFor(outboxRecord);
                var expiry = DateTime.Parse((string)metadata[Constants.Documents.Metadata.Expires], default, DateTimeStyles.RoundtripKind);

                Assert.That(expiry, Is.EqualTo(expectedExpiry).Within(maxExpectedExecutionTime));
            }
        }

        [Test]
        public async Task Should__not_store_expiry_in_metadata_if_time_to_keep_deduplication_data_is_infinite()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), Timeout.InfiniteTimeSpan, UseClusterWideTransactions);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;
            var outboxRecordId = "Outbox/TestEndpoint/" + incomingMessageId;

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                await session.StoreAsync(new OutboxRecord { MessageId = incomingMessageId, Dispatched = false, }, outboxRecordId);
                await session.SaveChangesAsync();
            }

            // act
            await persister.SetAsDispatched(incomingMessageId, context);

            // assert
            using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency())
            {
                var outboxRecord = await session.LoadAsync<OutboxRecord>(outboxRecordId);
                var metadata = session.Advanced.GetMetadataFor(outboxRecord);

                Assert.That(metadata.Keys.Contains(Constants.Documents.Metadata.Expires), Is.False);
            }
        }
    }
}
