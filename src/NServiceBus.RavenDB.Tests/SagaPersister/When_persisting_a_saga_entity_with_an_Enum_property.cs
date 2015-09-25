using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_persisting_a_saga_entity_with_an_Enum_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Enums_should_be_persisted()
    {
        var entity = new SagaData
        {
            Id = Guid.NewGuid(),
            Status = StatusEnum.AnotherStatus
        };

        IAsyncDocumentSession session;
        var options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var persister = new SagaPersister();
        await persister.Save(entity, options);
        await session.SaveChangesAsync();

        var savedEntity = await persister.Get<SagaData>(entity.Id, options);
        Assert.AreEqual(entity.Status, savedEntity.Status);
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
        public StatusEnum Status { get; set; }
    }

    enum StatusEnum
    {
        SomeStatus,
        AnotherStatus
    }

}