using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_trying_to_fetch_a_non_existing_saga_by_its_unique_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task It_should_return_null()
    {
        IDocumentSession session;
        var options = this.CreateContextWithSessionPresent(out session);
        var persister = new SagaPersister();
        Assert.Null(await persister.Get<SagaData>("UniqueString", Guid.NewGuid().ToString(), options));
    }

    class SomeSaga : Saga<SagaData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<Message>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        class Message
        {
            public string UniqueString { get; set; }
        }
    }

    class SagaData : IContainSagaData
    {
        public string UniqueString { get; set; }
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}