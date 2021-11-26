using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;

[TestFixture]
public class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga : RavenDBPersistenceTestBase
{
    [Test]
    public async Task It_should_persist_successfully()
    {
        var saga1Id = Guid.NewGuid();
        var uniqueString = Guid.NewGuid().ToString();

        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency().InContext(out var options))
        {
            var persister = new SagaPersister(new SagaPersistenceConfiguration(), UseClusterWideTransactions);
            var saga1 = new SagaData
            {
                Id = saga1Id,
                UniqueString = uniqueString
            };

            var synchronizedSession = new RavenDBSynchronizedStorageSession(session, options);

            await persister.Save(saga1, this.CreateMetadata<SomeSaga>(saga1), synchronizedSession, options);
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency().InContext(out var options))
        {
            var persister = new SagaPersister(new SagaPersistenceConfiguration(), UseClusterWideTransactions);
            var synchronizedSession = new RavenDBSynchronizedStorageSession(session, options);

            var saga = await persister.Get<SagaData>(saga1Id, synchronizedSession, options);
            await persister.Complete(saga, synchronizedSession, options);
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency().InContext(out var options))
        {
            var persister = new SagaPersister(new SagaPersistenceConfiguration(), UseClusterWideTransactions);
            var synchronizedSession = new RavenDBSynchronizedStorageSession(session, options);

            var saga2 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };

            await persister.Save(saga2, this.CreateMetadata<SomeSaga>(saga2), synchronizedSession, options);
            await session.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    class SomeSaga : Saga<SagaData>, IAmStartedByMessages<StartSaga>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartSaga>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        public Task Handle(StartSaga message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }

    sealed class SagaData : IContainSagaData
    {
        public string UniqueString { get; set; }
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}