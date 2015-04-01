namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.RavenDB.Outbox;
    using NUnit.Framework;
    using Raven.Abstractions.Exceptions;
    using Raven.Client.Exceptions;

    [TestFixture]
    public class When_adding_outbox_messages : RavenDBPersistenceTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            new OutboxRecordsIndex().Execute(store);
        }

        [Test]
        public async Task Should_throw_if__trying_to_insert_same_messageid_concurrently()
        {
            var persister = new OutboxPersister(store);
            var exception = await Catch<NonUniqueObjectException>(async () =>
            {
                using (var transaction = await persister.BeginTransaction(new ContextBag()))
                {
                    await persister.Store(new OutboxMessage("MySpecialId", new List<TransportOperation>()), transaction, new ContextBag());
                    await persister.Store(new OutboxMessage("MySpecialId", new List<TransportOperation>()), transaction, new ContextBag());
                    await transaction.Commit();
                }
            });

            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_throw_if__trying_to_insert_same_messageid()
        {
            var persister = new OutboxPersister(store);
            using (var transaction = await persister.BeginTransaction(new ContextBag()))
            {
                await persister.Store(new OutboxMessage("MySpecialId", new List<TransportOperation>()), transaction, new ContextBag());

                await transaction.Commit();
            }

            var exception = await Catch<ConcurrencyException>(async () =>
            {
                using (var transaction = await persister.BeginTransaction(new ContextBag()))
                {
                    await persister.Store(new OutboxMessage("MySpecialId", new List<TransportOperation>()), transaction, new ContextBag());

                    await transaction.Commit();
                }
            });
            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_save_with_not_dispatched()
        {
            var persister = new OutboxPersister(store);
            var id = Guid.NewGuid().ToString("N");
            var message = new OutboxMessage(id, new List<TransportOperation>
            {
                new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>())
            });

            using (var transaction = await persister.BeginTransaction(new ContextBag()))
            {
                await persister.Store(message, transaction, new ContextBag());

                await transaction.Commit();
            }

            var result = await persister.Get(id, new ContextBag());

            var operation = result.TransportOperations.Single();

            Assert.AreEqual(id, operation.MessageId);
        }

        [Test]
        public async Task Should_update_dispatched_flag()
        {
            var persister = new OutboxPersister(store);
            var id = Guid.NewGuid().ToString("N");
            var message = new OutboxMessage(id, new List<TransportOperation>
            {
                new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>())
            });

            using (var transaction = await persister.BeginTransaction(new ContextBag()))
            {
                await persister.Store(message, transaction, new ContextBag());

                await transaction.Commit();
            }
            await persister.SetAsDispatched(id, new ContextBag());

            WaitForIndexing(store);

            using (var s = store.OpenSession())
            {
                var result = s.Query<OutboxRecord>()
                    .SingleOrDefault(o => o.MessageId == id);

                Assert.NotNull(result);
                Assert.True(result.Dispatched);
            }
        }
    }
}