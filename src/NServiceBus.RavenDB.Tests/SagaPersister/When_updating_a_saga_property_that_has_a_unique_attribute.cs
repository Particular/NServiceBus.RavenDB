using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_updating_a_saga_property_that_has_a_unique_attribute : RavenDBPersistenceTestBase
{
    [Test]
    public async Task It_should_allow_the_update()
    {
        IAsyncDocumentSession session;
        var options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var persister = new SagaPersister();
        var uniqueString = Guid.NewGuid().ToString();
        var saga1 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };

        await persister.Save(saga1, options);
        await session.SaveChangesAsync();
        session.Dispose();

        options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var saga = await persister.Get<SagaData>(saga1.Id, options);
        saga.UniqueString = Guid.NewGuid().ToString();
        await persister.Update(saga, options);
        await session.SaveChangesAsync();
        session.Dispose();

        var saga2 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };

        //this should not blow since we changed the unique value in the previous saga
        options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        await persister.Save(saga2, options);
        await session.SaveChangesAsync();
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