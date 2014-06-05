using System;
using NServiceBus.RavenDB.Persistence;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Abstractions.Exceptions;

[TestFixture]
public class When_persisting_a_saga_with_the_same_unique_property_as_another_saga
{
    [Test]
    public void It_should_enforce_uniqueness()
    {
        using (var store = DocumentStoreBuilder.Build())
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
            
            Assert.Throws<ConcurrencyException>(() =>
            {
                var saga2 = new SagaData
                    {
                        Id = Guid.NewGuid(),
                        UniqueString = uniqueString
                    };
                persister.Save(saga2);
                factory.SaveChanges();
            });
        }
    }

    public class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        [Unique]
        public string UniqueString { get; set; }
    }
}