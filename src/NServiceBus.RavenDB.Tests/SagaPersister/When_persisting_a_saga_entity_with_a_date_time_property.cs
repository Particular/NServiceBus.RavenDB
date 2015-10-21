using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_persisting_a_saga_entity_with_a_DateTime_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Datetime_property_should_be_persisted()
    {
        var entity = new SagaData
        {
            Id = Guid.NewGuid(),
            DateTimeProperty = DateTime.Parse("12/02/2010 12:00:00.01")
        };
        IDocumentSession session;
        var options = this.CreateContextWithSessionPresent(out session);
        var persister = new SagaPersister();
        await persister.Save(entity, this.CreateMetadata<SomeSaga>(entity), options);
        session.SaveChanges();
        var savedEntity = await persister.Get<SagaData>(entity.Id, options);
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
        public DateTime DateTimeProperty { get; set; }
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}