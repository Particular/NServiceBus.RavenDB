using System;
using NServiceBus.RavenDB.Persistence;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.Saga;
using NUnit.Framework;

[TestFixture]
public class Saga_with_unique_property_set_to_null 
{
    [Test, ExpectedException(typeof(ArgumentNullException))]
    public void should_throw_a_ArgumentNullException()
    {
        using (var store = DocumentStoreBuilder.Build())
        {
            var saga1 = new SagaWithUniqueProperty
                {
                    Id = Guid.NewGuid(),
                    UniqueString = null
                };

            var factory = new RavenSessionFactory(store);
            var persister = new RavenSagaPersister(factory);
            persister.Save(saga1);
            factory.SaveChanges();
        }
    }
    public class SagaWithUniqueProperty : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        [Unique]
        public string UniqueString { get; set; }

    }
}