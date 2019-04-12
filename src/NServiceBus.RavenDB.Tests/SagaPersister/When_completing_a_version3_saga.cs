using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

[TestFixture]
public class When_completing_a_version3_saga : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Should_delete_the_unique_doc_properly()
    {
        var sagaId = Guid.NewGuid();

        IAsyncDocumentSession session;
        var options = this.CreateContextWithAsyncSessionPresent(out session);
        var persister = new SagaPersister();

        var sagaEntity = new SagaData
        {
            Id = sagaId,
            SomeId = Guid.NewGuid()
        };
        var synchronizedSession = new RavenDBSynchronizedStorageSession(session, true);

        await persister.Save(sagaEntity, this.CreateMetadata<SomeSaga>(sagaEntity), synchronizedSession, options);

        //session.Advanced.GetMetadataFor(sagaEntity).Remove("NServiceBus-UniqueDocId");
        await session.SaveChangesAsync().ConfigureAwait(false);

        var saga = await persister.Get<SagaData>(sagaId, synchronizedSession, options);
        await persister.Complete(saga, synchronizedSession, options);
        await session.SaveChangesAsync().ConfigureAwait(false);

        Assert.Null(await session.Query<SagaUniqueIdentity>().Customize(c => c.WaitForNonStaleResults()).SingleOrDefaultAsync(u => u.SagaId == sagaId));
    }


    class SomeSaga : Saga<SagaData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context)
        {
            return TaskEx.CompletedTask;
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