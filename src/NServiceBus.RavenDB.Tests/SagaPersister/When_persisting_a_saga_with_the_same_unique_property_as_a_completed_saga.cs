using System;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;

[TestFixture]
public class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga : RavenDBPersistenceTestBase
{
    [Test]
    public void It_should_persist_successfully()
    {
        var factory = new RavenSessionFactory(store);
        factory.ReleaseSession();
        var persister = new SagaPersister(factory);
        var uniqueString = Guid.NewGuid().ToString();
        var saga1 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };
        persister.Save(saga1);
        factory.SaveChanges();
        factory.ReleaseSession();

        var saga = persister.Get<SagaData>(saga1.Id);
        persister.Complete(saga);
        factory.SaveChanges();
        factory.ReleaseSession();

        var saga2 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };

        persister.Save(saga2);
        factory.SaveChanges();
    }

    sealed class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }

        public string Originator { get; set; }

        public string OriginalMessageId { get; set; }

        [Unique]
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string UniqueString { get; set; }
    }
}