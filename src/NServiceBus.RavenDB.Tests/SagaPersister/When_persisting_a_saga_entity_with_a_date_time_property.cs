using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_persisting_a_saga_entity_with_a_DateTime_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Datetime_property_should_be_persisted()
    {
        var entity = new SagaData
        {
            Id = Guid.NewGuid(),
            UniqueString = "SomeUniqueString",
            DateTimeProperty = DateTime.Parse("12/02/2010 12:00:00.01")
        };

        IAsyncDocumentSession session;
        var options = this.CreateContextWithAsyncSessionPresent(out session);
        var persister = new SagaPersister();
        var synchronizedSession = new RavenDBSynchronizedStorageSession(session, true);

        await persister.Save(entity, this.CreateMetadata<SomeSaga>(entity), synchronizedSession, options);
        await session.SaveChangesAsync().ConfigureAwait(false);
        var savedEntity = await persister.Get<SagaData>(entity.Id, synchronizedSession, options);
        Assert.AreEqual(entity.DateTimeProperty, savedEntity.DateTimeProperty);
    }

    class SomeSaga : Saga<SagaData>, IAmStartedByMessages<StartSaga>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartSaga>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        public Task Handle(StartSaga message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }

    class SagaData : IContainSagaData
    {
        public DateTime DateTimeProperty { get; set; }
        public Guid Id { get; set; }
        public string UniqueString { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}