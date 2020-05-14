namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using TimeoutData = NServiceBus.Timeout.Core.TimeoutData;
    using Timeout = NServiceBus.TimeoutPersisters.RavenDB.TimeoutData;

    public class When_adding_timeouts : RavenDBPersistenceTestBase
    {
        [Test]
        public async Task Add_WhenNoIdProvided_ShouldSetDbGeneratedTimeoutId()
        {
            var persister = new TimeoutPersister(store);
            var timeout = new TimeoutData { Id = null };

            await persister.Add(timeout, new ContextBag());
            Assert.IsNotNull(timeout.Id);

            var result = await persister.Peek(timeout.Id, new ContextBag());
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task Add_WhenIdProvided_ShouldOverrideGivenId()
        {
            var persister = new TimeoutPersister(store);

            var timeoutId = Guid.NewGuid().ToString();
            var timeout = new TimeoutData { Id = timeoutId };

            await persister.Add(timeout, new ContextBag());
            Assert.AreNotEqual(timeoutId, timeout.Id);

            var result = await persister.Peek(timeoutId, new ContextBag());
            Assert.IsNull(result);
        }

        [Test]
        public async Task Add_ShouldStoreSchemaVersion()
        {
            var persister = new TimeoutPersister(store);

            var timeoutId = Guid.NewGuid().ToString();
            var timeout = new TimeoutData { Id = timeoutId };

            await persister.Add(timeout, new ContextBag());

            WaitForIndexing();

            using (var session = store.OpenAsyncSession())
            {
                var subscriptions = await session
                    .Query<TimeoutData>()
                    .SingleOrDefaultAsync();

                var metadata = session.Advanced.GetMetadataFor(subscriptions);

                Assert.AreEqual(Timeout.SchemaVersion.ToString(3), metadata[SessionVersionExtensions.TimeoutSchemaVersionMetadataKey]);
            }
        }
    }
}