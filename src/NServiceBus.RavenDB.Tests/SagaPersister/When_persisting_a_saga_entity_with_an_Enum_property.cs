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

        var context = this.CreateContextWithAsyncSessionPresent(out session);
        var persister = new SagaPersister();
        await persister.Save(entity, this.CreateMetadata<SomeSaga>(entity), context);
        await session.SaveChangesAsync().ConfigureAwait(false);

        var savedEntity = await persister.Get<SagaData>(entity.Id, context);
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
        public StatusEnum Status { get; set; }
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

    enum StatusEnum
    {
        SomeStatus,
        AnotherStatus
    }
}