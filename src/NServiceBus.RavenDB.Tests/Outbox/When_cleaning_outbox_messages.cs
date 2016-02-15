﻿namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.RavenDB.Outbox;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Raven.Client;

    [TestFixture]
    public class When_cleaning_outbox_messages : RavenDBPersistenceTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            new OutboxRecordsIndex().Execute(store);
        }

        [Test]
        public async Task Should_delete_all_OutboxRecords_that_have_been_dispatched()
        {
            var id = Guid.NewGuid().ToString("N");
            var context = new ContextBag();

            var persister = new OutboxPersister(store, new EndpointName("TestEndpoint"));

            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(new OutboxMessage("NotDispatched", new List<TransportOperation>()), transaction, context);

                await transaction.Commit();
            }

            var outboxMessage = new OutboxMessage(id, new List<TransportOperation>
                {
                    new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>())
                });


            using (var transaction = await persister.BeginTransaction(context))
            {
                await persister.Store(outboxMessage, transaction, context);
                await transaction.Commit();
            }


            await persister.SetAsDispatched(id, context);
            Thread.Sleep(TimeSpan.FromSeconds(1)); //Need to wait for dispatch logic to finish

            //WaitForUserToContinueTheTest(store);
            WaitForIndexing(store);

            var cleaner = new OutboxRecordsCleaner
            {
                DocumentStore = store
            };

            await cleaner.RemoveEntriesOlderThan(DateTime.UtcNow.AddMinutes(1));

            using (var s = store.OpenAsyncSession())
            {
                var result = await s.Query<OutboxRecord>().ToListAsync();

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("NotDispatched", result[0].MessageId);
            }
        }
    }
}