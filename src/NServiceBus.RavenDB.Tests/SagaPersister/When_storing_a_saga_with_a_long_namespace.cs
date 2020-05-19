using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;

[TestFixture]
public class When_storing_a_saga_with_a_long_namespace : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Should_not_generate_a_to_long_unique_property_id()
    {
        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency().InContext(out var options))
        {
            var persister = new SagaPersister();
            var uniqueString = Guid.NewGuid().ToString();
            var saga = new SagaWithUniquePropertyAndALongNamespace
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };
            var synchronizedSession = new RavenDBSynchronizedStorageSession(session, new ContextBag());

            await persister.Save(saga, this.CreateMetadata<SomeSaga>(saga), synchronizedSession, options);
            await session.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    class SomeSaga : Saga<SagaWithUniquePropertyAndALongNamespace>, IAmStartedByMessages<StartSaga>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithUniquePropertyAndALongNamespace> mapper)
        {
            mapper.ConfigureMapping<StartSaga>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        public Task Handle(StartSaga message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }

    class SagaWithUniquePropertyAndALongNamespace : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string UniqueString { get; set; }
    }
}