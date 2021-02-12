using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;

[TestFixture]
public class When_persisting_a_saga_entity_with_a_concrete_class_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Public_setters_and_getters_of_concrete_classes_should_be_persisted()
    {
        var entity = new SagaData
        {
            Id = Guid.NewGuid(),
            UniqueString = "SomeUniqueString",
            TestComponent = new TestComponent
            {
                Property = "Prop"
            }
        };

        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency().InContext(out var options))
        {
            var persister = new SagaPersister(new SagaPersistenceConfiguration(), CreateTestSessionOpener());
            var synchronizedSession = new RavenDBSynchronizedStorageSession(session, options);

            await persister.Save(entity, this.CreateMetadata<SomeSaga>(entity), synchronizedSession, options);
            await session.SaveChangesAsync().ConfigureAwait(false);

            var savedEntity = await persister.Get<SagaData>(entity.Id, synchronizedSession, options);

            Assert.AreEqual(entity.TestComponent.Property, savedEntity.TestComponent.Property);
            Assert.AreEqual(entity.TestComponent.AnotherProperty, savedEntity.TestComponent.AnotherProperty);
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
        public TestComponent TestComponent { get; set; }
        public Guid Id { get; set; }
        public string UniqueString { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

    class TestComponent
    {
        public string Property { get; set; }
        public string AnotherProperty { get; set; }
    }
}