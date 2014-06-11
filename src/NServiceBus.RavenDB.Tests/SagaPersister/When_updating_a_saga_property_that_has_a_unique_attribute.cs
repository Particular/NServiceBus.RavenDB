using System;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;

[TestFixture]
public class When_updating_a_saga_property_that_has_a_unique_attribute 
{
    [Test]
    public void It_should_allow_the_update()
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
            saga.UniqueString = Guid.NewGuid().ToString();
            persister.Update(saga);
            factory.SaveChanges();
            factory.ReleaseSession();

            var saga2 = new SagaData
                {
                    Id = Guid.NewGuid(),
                    UniqueString = uniqueString
                };

            //this should not blow since we changed the unique value in the previous saga
            persister.Save(saga2);
            factory.SaveChanges();
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