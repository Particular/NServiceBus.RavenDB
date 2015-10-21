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
public class When_completing_a_version3_saga : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Should_delete_the_unique_doc_properly()
    {
        var sagaId = Guid.NewGuid();

        IDocumentSession session;
        var options = this.CreateContextWithSessionPresent(out session);
        var persister = new SagaPersister();

        var sagaEntity = new SagaData
        {
            Id = sagaId,
            SomeId = Guid.NewGuid()
        };

        await persister.Save(sagaEntity, this.CreateMetadata<SomeSaga>(sagaEntity), options);

        session.Advanced.GetMetadataFor(sagaEntity).Remove("NServiceBus-UniqueDocId");
        session.SaveChanges();


        var saga = await persister.Get<SagaData>(sagaId, options);
        await persister.Complete(saga, options);
        session.SaveChanges();

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