using System;
using NServiceBus.RavenDB.Persistence;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.Saga;
using NUnit.Framework;

[TestFixture]
public class When_storing_a_saga_with_a_long_namespace
{
    [Test]
    public void Should_not_generate_a_to_long_unique_property_id()
    {
        using (var store = DocumentStoreBuilder.Build())
        {
            var factory = new RavenSessionFactory(store);
            factory.ReleaseSession();
            var persister = new RavenSagaPersister(factory);
            var uniqueString = Guid.NewGuid().ToString();
            var saga = new SagaWithUniquePropertyAndALongNamespace
                {
                    Id = Guid.NewGuid(),
                    UniqueString = uniqueString
                };
            persister.Save(saga);
            factory.SaveChanges();
        }
    }

    public class SagaWithUniquePropertyAndALongNamespace : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        [Unique]
        public string UniqueString { get; set; }

    }
}