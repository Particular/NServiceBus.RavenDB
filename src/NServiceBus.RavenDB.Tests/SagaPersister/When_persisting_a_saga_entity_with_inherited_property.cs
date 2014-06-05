using System;
using NServiceBus.RavenDB.Persistence;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;

[TestFixture]
public class When_persisting_a_saga_entity_with_inherited_property
{

    [Test]
    public void Inherited_property_classes_should_be_persisted()
    {
        using (var store = DocumentStoreBuilder.Build())
        {

            var factory = new RavenSessionFactory(store);
            factory.ReleaseSession();
            var persister = new SagaPersister(factory);
            var entity = new SagaData
                {
                    Id = Guid.NewGuid(),
                    PolymorphicRelatedProperty = new PolymorphicProperty
                        {
                            SomeInt = 9
                        }
                };
            persister.Save(entity);
            factory.SaveChanges();
            
            var savedEntity = persister.Get<SagaData>(entity.Id);
            var expected = (PolymorphicProperty) entity.PolymorphicRelatedProperty;
            var actual = (PolymorphicProperty) savedEntity.PolymorphicRelatedProperty;
            Assert.AreEqual(expected.SomeInt, actual.SomeInt);
        }
    }

    public class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public PolymorphicPropertyBase PolymorphicRelatedProperty { get; set; }
    }

    public class PolymorphicProperty : PolymorphicPropertyBase
    {
        public int SomeInt { get; set; }
    }

    public class PolymorphicPropertyBase
    {
        public virtual Guid Id { get; set; }
    }

}