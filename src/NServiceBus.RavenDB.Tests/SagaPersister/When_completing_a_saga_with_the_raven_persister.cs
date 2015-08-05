using System;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_completing_a_saga_with_the_raven_persister : RavenDBPersistenceTestBase
{

    [Test]
    public void Should_delete_the_saga()
    {
        var sagaId = Guid.NewGuid();

        IDocumentSession session;
        var options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var persister = new SagaPersister();
        persister.Save(new SagaData
        {
            Id = sagaId
        }, options);
        session.SaveChanges();

        var saga = persister.Get<SagaData>(sagaId, options);
        persister.Complete(saga, options);
        session.SaveChanges();

        Assert.Null(persister.Get<SagaData>(sagaId, options));
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