using System;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;

[TestFixture]
public class When_trying_to_fetch_a_non_existing_saga_by_its_unique_property 
{
    [Test]
    public void It_should_return_null()
    {

        using (var store = DocumentStoreBuilder.Build())
        {
            var factory = new RavenSessionFactory(store);
            factory.ReleaseSession();
            var persister = new SagaPersister(factory);
            Assert.Null(persister.Get<SagaData>("UniqueString", Guid.NewGuid().ToString()));
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