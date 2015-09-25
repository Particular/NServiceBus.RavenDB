using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_completing_a_saga_with_the_raven_persister : RavenDBPersistenceTestBase
{

    [Test]
    public async Task Should_delete_the_saga()
    {
        var sagaId = Guid.NewGuid();

        IAsyncDocumentSession session;
        var options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var persister = new SagaPersister();
        await persister.Save(new SagaData
        {
            Id = sagaId
        }, options);
        await session.SaveChangesAsync();

        var saga = await persister.Get<SagaData>(sagaId, options);
        await persister.Complete(saga, options);
        await session.SaveChangesAsync();

        Assert.Null(await persister.Get<SagaData>(sagaId, options));
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
    }
}