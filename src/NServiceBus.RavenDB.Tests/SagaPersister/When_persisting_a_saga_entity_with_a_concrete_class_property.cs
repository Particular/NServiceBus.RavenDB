using System;
using NServiceBus.RavenDB.Persistence;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.Saga;
using NUnit.Framework;

[TestFixture]
public class When_persisting_a_saga_entity_with_a_concrete_class_property
{

    [Test]
    public void Public_setters_and_getters_of_concrete_classes_should_be_persisted()
    {

        using (var store = DocumentStoreBuilder.Build())
        {
            var entity = new SagaData
                {
                    Id = Guid.NewGuid(),
                    TestComponent = new TestComponent
                        {
                            Property = "Prop"
                        }
                };
            var factory = new RavenSessionFactory(new StoreAccessor(store));
            factory.ReleaseSession();
            var persister = new RavenSagaPersister(factory);
            persister.Save(entity);
            factory.SaveChanges();
            var savedEntity = persister.Get<SagaData>(entity.Id);
            Assert.AreEqual(entity.TestComponent.Property, savedEntity.TestComponent.Property);
            Assert.AreEqual(entity.TestComponent.AnotherProperty, savedEntity.TestComponent.AnotherProperty);
        }
    }

    public class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public TestComponent TestComponent { get; set; }
    }

    public class TestComponent
    {
        public string Property { get; set; }
        public string AnotherProperty { get; set; }
    }
}