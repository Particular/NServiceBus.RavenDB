using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;

[TestFixture]
public class When_persisting_a_saga_entity_with_a_DateTime_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Datetime_property_should_be_persisted()
    {
        var entity = new SagaData
        {
            Id = Guid.NewGuid(),
            UniqueString = "SomeUniqueString",
            DateTimeProperty = DateTime.Parse("12/02/2010 12:00:00.01")
        };

        using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency().InContext(out var options))
        {
            var persister = new SagaPersister(new SagaPersistenceConfiguration(), UseClusterWideTransactions);
            var synchronizedSession = await session.CreateSynchronizedSession(options);

            await persister.Save(entity, this.CreateMetadata<SomeSaga>(entity), synchronizedSession, options);
            await session.SaveChangesAsync().ConfigureAwait(false);
            var savedEntity = await persister.Get<SagaData>(entity.Id, synchronizedSession, options);
            Assert.That(savedEntity.DateTimeProperty, Is.EqualTo(entity.DateTimeProperty));
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

    class SagaData : IContainSagaData
    {
        public DateTime DateTimeProperty { get; set; }
        public Guid Id { get; set; }
        public string UniqueString { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}