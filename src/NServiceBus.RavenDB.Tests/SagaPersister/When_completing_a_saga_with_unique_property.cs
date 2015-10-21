using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_completing_a_saga_with_unique_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Should_delete_the_saga_and_the_unique_doc()
    {
        var sagaId = Guid.NewGuid();

        IDocumentSession session;
        var options = this.CreateContextWithSessionPresent(out session);
        var persister = new SagaPersister();
        var entity = new SagaData
        {
            Id = sagaId
        };

        await persister.Save(entity, this.CreateMetadata<SomeSaga>(entity), options);
        session.SaveChanges();

        var saga = await persister.Get<SagaData>(sagaId, options);
        await persister.Complete(saga, options);
        session.SaveChanges();

        Assert.Null(await persister.Get<SagaData>(sagaId, options));
        Assert.Null(session.Query<SagaUniqueIdentity>().Customize(c => c.WaitForNonStaleResults()).SingleOrDefault(u => u.SagaId == sagaId));
    }


    class SomeSaga : Saga<SagaData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartMessage>(m => m.SomeId).ToSaga(s => s.SomeId);
        }
    }

    class StartMessage : IMessage
    {
        public Guid SomeId { get; set; }
    }

    class SagaData : IContainSagaData
    {
        public Guid SomeId { get; set; }
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}