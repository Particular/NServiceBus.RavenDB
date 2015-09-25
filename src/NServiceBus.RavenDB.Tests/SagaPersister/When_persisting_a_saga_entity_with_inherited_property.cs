using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_persisting_a_saga_entity_with_inherited_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Inherited_property_classes_should_be_persisted()
    {
        IAsyncDocumentSession session;
        var options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var persister = new SagaPersister();
        var entity = new SagaData
            {
                Id = Guid.NewGuid(),
                PolymorphicRelatedProperty = new PolymorphicProperty
                    {
                        SomeInt = 9
                    }
            };
        await persister.Save(entity, options);
        await session.SaveChangesAsync();

        var savedEntity = await persister.Get<SagaData>(entity.Id, options);
        var expected = (PolymorphicProperty)entity.PolymorphicRelatedProperty;
        var actual = (PolymorphicProperty)savedEntity.PolymorphicRelatedProperty;
        Assert.AreEqual(expected.SomeInt, actual.SomeInt);
    }

    class SomeSaga : Saga<SagaData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

    class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public PolymorphicPropertyBase PolymorphicRelatedProperty { get; set; }
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