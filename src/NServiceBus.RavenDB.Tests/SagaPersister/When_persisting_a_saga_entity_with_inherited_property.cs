using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;

[TestFixture]
public class When_persisting_a_saga_entity_with_inherited_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Inherited_property_classes_should_be_persisted()
    {
        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency().InContext(out var options))
        {
            var persister = new SagaPersister(new SagaPersistenceConfiguration());
            var entity = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = "SomeUniqueString",
                PolymorphicRelatedProperty = new PolymorphicProperty
                {
                    SomeInt = 9
                }
            };
            var synchronizedSession = new RavenDBSynchronizedStorageSession(session, options);

            await persister.Save(entity, this.CreateMetadata<SomeSaga>(entity), synchronizedSession, options);
            await session.SaveChangesAsync().ConfigureAwait(false);

            var savedEntity = await persister.Get<SagaData>(entity.Id, synchronizedSession, options);
            var expected = (PolymorphicProperty)entity.PolymorphicRelatedProperty;
            var actual = (PolymorphicProperty)savedEntity.PolymorphicRelatedProperty;
            Assert.AreEqual(expected.SomeInt, actual.SomeInt);
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
        public PolymorphicPropertyBase PolymorphicRelatedProperty { get; set; }
        public Guid Id { get; set; }
        public string UniqueString { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

    class PolymorphicProperty : PolymorphicPropertyBase
    {
        public int SomeInt { get; set; }
    }

    class PolymorphicPropertyBase
    {
        public virtual Guid Id { get; set; }
    }
}