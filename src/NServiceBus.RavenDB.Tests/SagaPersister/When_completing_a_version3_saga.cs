using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;
using Raven.Client.Documents;

[TestFixture]
public class When_completing_a_version3_saga : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Should_delete_the_unique_doc_properly()
    {
        var sagaId = Guid.NewGuid();
        using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency().InContext(out var options))
        {
            var persister = new SagaPersister(new SagaPersistenceConfiguration(), UseClusterWideTransactions);

            var sagaEntity = new SagaData
            {
                Id = sagaId,
                SomeId = Guid.NewGuid()
            };
            var synchronizedSession = await session.CreateSynchronizedSession(options);

            await persister.Save(sagaEntity, this.CreateMetadata<SomeSaga>(sagaEntity), synchronizedSession, options);

            await session.SaveChangesAsync().ConfigureAwait(false);

            var saga = await persister.Get<SagaData>(sagaId, synchronizedSession, options);
            await persister.Complete(saga, synchronizedSession, options);
            await session.SaveChangesAsync().ConfigureAwait(false);

            Assert.That(await session.Query<SagaUniqueIdentity>().Customize(c => c.WaitForNonStaleResults()).SingleOrDefaultAsync(u => u.SagaId == sagaId), Is.Null);
        }
    }


    class SomeSaga : Saga<SagaData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.MapSaga(s => s.SomeId).ToMessage<StartMessage>(m => m.SomeId);
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