using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;

[TestFixture]
public class When_trying_to_fetch_a_non_existing_saga_by_its_unique_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task It_should_return_null()
    {
        using (var session = this.CreateAsyncSessionInContext(out var options))
        {
            var persister = new SagaPersister();
            var synchronizedSession = new RavenDBSynchronizedStorageSession(session);

            Assert.Null(await persister.Get<SagaData>("UniqueString", Guid.NewGuid().ToString(), synchronizedSession, options));
        }
    }

    class SomeSaga : Saga<SagaData>, IAmStartedByMessages<StartSaga>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<Message>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        public Task Handle(StartSaga message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
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