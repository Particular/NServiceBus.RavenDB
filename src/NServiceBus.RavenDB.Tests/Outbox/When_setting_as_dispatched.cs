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
        public override void SetUp()
        {
            base.SetUp();
            new OutboxRecordsIndex().Execute(store);
        }

        [Test]
        public async Task Should_set_outbox_record_as_dispatched()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default);
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
                Assert.NotNull(outboxRecord.DispatchedAt);
                Assert.IsEmpty(outboxRecord.TransportOperations);
            }
        }

        [Test]
        public async Task Should_store_schema_version_in_metadata()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), default);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = OpenAsyncSession())
            {
                await session.StoreAsync(
                    new OutboxRecord { MessageId = incomingMessageId, Dispatched = false, },
                    "Outbox/TestEndpoint/" + incomingMessageId);

                await session.SaveChangesAsync();
            }

            // act
            await persister.SetAsDispatched(incomingMessageId, context);

            // assert
            using (var session = OpenAsyncSession())
            {
                var outboxRecord = await session.LoadAsync<OutboxRecord>("Outbox/TestEndpoint/" + incomingMessageId);
                var metadata = session.Advanced.GetMetadataFor(outboxRecord);

                Assert.AreEqual(OutboxRecord.SchemaVersion, metadata[SchemaVersionExtensions.OutboxRecordSchemaVersionMetadataKey]);
            }
        }

        [Test]
        public async Task Should_store_expiry_in_metadata_if_time_to_keep_deduplication_data_is_finite()
        {
            // arrange
            var timeToKeepDeduplicationData = TimeSpan.FromSeconds(60);
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), timeToKeepDeduplicationData);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = OpenAsyncSession())
            {
                await session.StoreAsync(
                    new OutboxRecord { MessageId = incomingMessageId, Dispatched = false, },
                    "Outbox/TestEndpoint/" + incomingMessageId);

                await session.SaveChangesAsync();
            }

            var expectedExpiry = DateTime.UtcNow.Add(timeToKeepDeduplicationData);
            var maxExpectedExecutionTime = TimeSpan.FromMinutes(15);

            // act
            await persister.SetAsDispatched(incomingMessageId, context);

            // assert
            using (var session = OpenAsyncSession())
            {
                var outboxRecord = await session.LoadAsync<OutboxRecord>("Outbox/TestEndpoint/" + incomingMessageId);
                var metadata = session.Advanced.GetMetadataFor(outboxRecord);
                var expiry = DateTime.Parse((string)metadata[Constants.Documents.Metadata.Expires], default, DateTimeStyles.RoundtripKind);

                Assert.That(expiry, Is.EqualTo(expectedExpiry).Within(maxExpectedExecutionTime));
            }
        }

        [Test]
        public async Task Should__not_store_expiry_in_metadata_if_time_to_keep_deduplication_data_is_infinite()
        {
            // arrange
            var persister = new OutboxPersister("TestEndpoint", CreateTestSessionOpener(), Timeout.InfiniteTimeSpan);
            var context = new ContextBag();
            var incomingMessageId = SimulateIncomingMessage(context).MessageId;

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = OpenAsyncSession())
            {
                await session.StoreAsync(
                    new OutboxRecord { MessageId = incomingMessageId, Dispatched = false, },
                    "Outbox/TestEndpoint/" + incomingMessageId);

                await session.SaveChangesAsync();
            }

            // act
            await persister.SetAsDispatched(incomingMessageId, context);

            // assert
            using (var session = OpenAsyncSession())
            {
                var outboxRecord = await session.LoadAsync<OutboxRecord>("Outbox/TestEndpoint/" + incomingMessageId);
                var metadata = session.Advanced.GetMetadataFor(outboxRecord);

                Assert.False(metadata.Keys.Contains(Constants.Documents.Metadata.Expires));
            }
        }
    }
}
