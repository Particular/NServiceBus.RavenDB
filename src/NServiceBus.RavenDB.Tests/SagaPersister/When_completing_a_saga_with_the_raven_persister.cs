using System;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;

[TestFixture]
public class When_completing_a_saga_with_the_raven_persister : RavenDBPersistenceTestBase
{

    [Test]
    public void Should_delete_the_saga()
    {
        var sagaId = Guid.NewGuid();

        var factory = new RavenSessionFactory(store);
        factory.ReleaseSession();
        var persister = new SagaPersister(factory);
        persister.Save(new SagaData
            {
                Id = sagaId
            });
        factory.SaveChanges();

        var saga = persister.Get<SagaData>(sagaId);
        persister.Complete(saga);
        factory.SaveChanges();

        Assert.Null(persister.Get<SagaData>(sagaId));
    }

    class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}