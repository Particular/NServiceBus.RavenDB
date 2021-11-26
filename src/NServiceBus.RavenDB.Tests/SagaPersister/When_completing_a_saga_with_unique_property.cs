using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;
using Raven.Client.Documents;

[TestFixture]
public class When_completing_a_saga_with_unique_property : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Should_delete_the_saga_and_the_unique_doc()
    {
        var sagaId = Guid.NewGuid();

        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency().InContext(out var options))
        {
            var persister = new SagaPersister(new SagaPersistenceConfiguration(), UseClusterWideTransactions);
            var entity = new SagaData
            {
                Id = sagaId
            };

            // Save a saga
            using (var synchronizedSession = new RavenDBSynchronizedStorageSession(session, options))
            {
                await persister.Save(entity, this.CreateMetadata<SomeSaga>(entity), synchronizedSession, options);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }

            // Delete the saga
            using (var synchronizedSession = new RavenDBSynchronizedStorageSession(session, options))
            {
                var saga = await persister.Get<SagaData>(sagaId, synchronizedSession, options);
                await persister.Complete(saga, synchronizedSession, options);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }

            // Check to see if the saga is gone
            SagaData testSaga;
            SagaUniqueIdentity testIdentity;
            using (var synchronizedSession = new RavenDBSynchronizedStorageSession(session, options))
            {
                testSaga = await persister.Get<SagaData>(sagaId, synchronizedSession, options).ConfigureAwait(false);
                testIdentity = await session.Query<SagaUniqueIdentity>().Customize(c => c.WaitForNonStaleResults()).SingleOrDefaultAsync(u => u.SagaId == sagaId).ConfigureAwait(false);
            }

            Assert.Null(testSaga);
            Assert.Null(testIdentity);
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