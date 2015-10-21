using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga : RavenDBPersistenceTestBase
{
    [Test]
    public async Task It_should_persist_successfully()
    {
        IDocumentSession session;
        var options = this.CreateContextWithSessionPresent(out session);
        var persister = new SagaPersister();
        var uniqueString = Guid.NewGuid().ToString();
        var saga1 = new SagaData
        {
            Id = Guid.NewGuid(),
            UniqueString = uniqueString
        };
        await persister.Save(saga1, this.CreateMetadata<SomeSaga>(saga1), options);
        session.SaveChanges();
        session.Dispose();

        options = this.CreateContextWithSessionPresent(out session);
        var saga = await persister.Get<SagaData>(saga1.Id, options);
        await persister.Complete(saga, options);
        session.SaveChanges();
        session.Dispose();

        options = this.CreateContextWithSessionPresent(out session);
        var saga2 = new SagaData
        {
            Id = Guid.NewGuid(),
            UniqueString = uniqueString
        };

        await persister.Save(saga2, this.CreateMetadata<SomeSaga>(saga2), options);
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
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string UniqueString { get; set; }
        public Guid Id { get; set; }

        public string Originator { get; set; }

        public string OriginalMessageId { get; set; }
    }
}