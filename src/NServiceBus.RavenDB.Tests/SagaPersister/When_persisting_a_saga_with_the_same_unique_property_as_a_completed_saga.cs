using System;
using NServiceBus.RavenDB.Persistence;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.Saga;
using NUnit.Framework;

[TestFixture]
public class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga 
{
    [Test]
    public void It_should_persist_successfully()
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
    }

    public class SagaData : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }
    }
}