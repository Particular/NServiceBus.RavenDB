using System;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_storing_a_saga_with_a_long_namespace : RavenDBPersistenceTestBase
{
    [Test]
    public void Should_not_generate_a_to_long_unique_property_id()
    {
        IDocumentSession session;
        var options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var persister = new SagaPersister();
        var uniqueString = Guid.NewGuid().ToString();
        var saga = new SagaWithUniquePropertyAndALongNamespace
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };
        persister.Save(saga, options);
        session.SaveChanges();
    }

    class SomeSaga : Saga<SagaWithUniquePropertyAndALongNamespace>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithUniquePropertyAndALongNamespace> mapper)
        {
        }
    }

    class SagaWithUniquePropertyAndALongNamespace : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string UniqueString { get; set; }
    }
}