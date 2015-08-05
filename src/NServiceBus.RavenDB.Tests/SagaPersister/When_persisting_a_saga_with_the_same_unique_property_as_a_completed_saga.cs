using System;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga : RavenDBPersistenceTestBase
{
    [Test]
    public void It_should_persist_successfully()
    {
        IDocumentSession session;
        var options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var persister = new SagaPersister();
        var uniqueString = Guid.NewGuid().ToString();
        var saga1 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };
        persister.Save(saga1, options);
        session.SaveChanges();
        session.Dispose();

        options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var saga = persister.Get<SagaData>(saga1.Id, options);
        persister.Complete(saga, options);
        session.SaveChanges();
        session.Dispose();

        options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var saga2 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };

        persister.Save(saga2, options);
        session.SaveChanges();
    }

    class SomeSaga : Saga<SagaData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

    sealed class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }

        public string Originator { get; set; }

        public string OriginalMessageId { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string UniqueString { get; set; }
    }
}