using System;
using NServiceBus.RavenDB.Persistence;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.Saga;
using NUnit.Framework;

[TestFixture]
public class When_completing_a_saga_with_the_raven_persister
{

    [Test]
    public void Should_delete_the_saga()
    {
        using (var store = DocumentStoreBuilder.Build())
        {
            var sagaId = Guid.NewGuid();

            var factory = new RavenSessionFactory(store);
            factory.ReleaseSession();
            var persister = new RavenSagaPersister(factory);
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
    }

    public class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}