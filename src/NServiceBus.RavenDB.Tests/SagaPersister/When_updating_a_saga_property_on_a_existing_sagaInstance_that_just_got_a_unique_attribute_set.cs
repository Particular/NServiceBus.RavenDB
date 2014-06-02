using System;
using NServiceBus.RavenDB.Persistence;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.Saga;
using NUnit.Framework;

[TestFixture]
public class When_updating_a_saga_property_on_a_existing_sagaInstance_that_just_got_a_unique_attribute_set
{
    [Test]
    public void It_should_set_the_attribute_and_allow_the_update()
    {

        using (var store = DocumentStoreBuilder.Build())
        {
            var factory = new RavenSessionFactory(store);
            factory.ReleaseSession();
            var persister = new SagaPersister(factory);
            var uniqueString = Guid.NewGuid().ToString();

            var anotherUniqueString = Guid.NewGuid().ToString();

            var saga1 = new SagaData
                {
                    Id = Guid.NewGuid(),
                    UniqueString = uniqueString,
                    NonUniqueString = "notUnique"
                };

            persister.Save(saga1);
            factory.SaveChanges();
            factory.ReleaseSession();

            using (var session = store.OpenSession())
            {
                //fake that the attribute was just added by removing the metadata
                session.Advanced.GetMetadataFor(saga1).Remove(SagaPersister.UniqueValueMetadataKey);
                session.SaveChanges();
            }

            var saga = persister.Get<SagaData>(saga1.Id);
            saga.UniqueString = anotherUniqueString;
            persister.Update(saga);
            factory.SaveChanges();
            factory.ReleaseSession();

            using (var session = store.OpenSession())
            {
                var value = session.Advanced.GetMetadataFor(saga1)[SagaPersister.UniqueValueMetadataKey].ToString();
                Assert.AreEqual(anotherUniqueString, value);
            }
        }
    }

    public class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        [Unique]
        public string UniqueString { get; set; }

        public string NonUniqueString { get; set; }
    }
}