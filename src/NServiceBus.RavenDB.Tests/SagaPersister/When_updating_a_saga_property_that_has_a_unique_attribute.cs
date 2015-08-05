using System;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_updating_a_saga_property_that_has_a_unique_attribute : RavenDBPersistenceTestBase
{
    [Test]
    public void It_should_allow_the_update()
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
        saga.UniqueString = Guid.NewGuid().ToString();
        persister.Update(saga, options);
        session.SaveChanges();
        session.Dispose();

        var saga2 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };

        //this should not blow since we changed the unique value in the previous saga
        options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        persister.Save(saga2, options);
        session.SaveChanges();
    }

    class SomeSaga : Saga<SagaData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

    class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string UniqueString { get; set; }
    }
}