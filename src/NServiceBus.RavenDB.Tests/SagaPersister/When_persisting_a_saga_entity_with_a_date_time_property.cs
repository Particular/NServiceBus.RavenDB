using System;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_persisting_a_saga_entity_with_a_DateTime_property : RavenDBPersistenceTestBase
{
    [Test]
    public void Datetime_property_should_be_persisted()
    {
        var entity = new SagaData
            {
                Id = Guid.NewGuid(),
                DateTimeProperty = DateTime.Parse("12/02/2010 12:00:00.01")
            };
        IDocumentSession session;
        var options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var persister = new SagaPersister();
        persister.Save(entity, options);
        session.SaveChanges();
        var savedEntity = persister.Get<SagaData>(entity.Id, options);
        Assert.AreEqual(entity.DateTimeProperty, savedEntity.DateTimeProperty);
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
        public DateTime DateTimeProperty { get; set; }
    }
}