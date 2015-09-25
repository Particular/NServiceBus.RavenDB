using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_persisting_a_saga_entity_with_a_concrete_class_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Public_setters_and_getters_of_concrete_classes_should_be_persisted()
    {
        var entity = new SagaData
                {
                    Id = Guid.NewGuid(),
                    TestComponent = new TestComponent
                        {
                            Property = "Prop"
                        }
                };

        IAsyncDocumentSession session;
        var options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var persister = new SagaPersister();
        await persister.Save(entity, options);
        await session.SaveChangesAsync();
        var savedEntity = await persister.Get<SagaData>(entity.Id, options);
        Assert.AreEqual(entity.TestComponent.Property, savedEntity.TestComponent.Property);
        Assert.AreEqual(entity.TestComponent.AnotherProperty, savedEntity.TestComponent.AnotherProperty);
    }

    class SomeSaga : Saga<SagaData> {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

    class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public TestComponent TestComponent { get; set; }
    }

    class TestComponent
    {
        public string Property { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string AnotherProperty { get; set; }
    }
}