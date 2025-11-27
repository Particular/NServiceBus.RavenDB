using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;

[TestFixture]
public class When_persisting_a_saga_entity : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Schema_version_should_be_persisted()
    {
        // arrange
        var entity = new SagaData
        {
            Id = Guid.NewGuid(),
            UniqueString = "SomeUniqueString",
        };

        var persister = new SagaPersister(new SagaPersistenceConfiguration(), UseClusterWideTransactions);
        using (var session = store.OpenAsyncSession(GetSessionOptions()).UsingOptimisticConcurrency().InContext(out var context))
        {
            var synchronizedSession = await session.CreateSynchronizedSession(new ContextBag());

            // act
            await persister.Save(entity, this.CreateMetadata<SomeSaga>(entity), synchronizedSession, context);
            await session.SaveChangesAsync();

            // assert
            var sagaDataContainerId = SagaPersister.DocumentIdForSagaData(session, entity);
            var sagaDataContainer = await session.LoadAsync<SagaDataContainer>(sagaDataContainerId);
            var sagaDataContainerMetadata = session.Advanced.GetMetadataFor(sagaDataContainer);

            var sagaUniqueIdentityId = sagaDataContainer.IdentityDocId;
            var sagaUniqueIdentity = await session.LoadAsync<SagaUniqueIdentity>(sagaUniqueIdentityId);
            var sagaUniqueIdentityMetadata = session.Advanced.GetMetadataFor(sagaUniqueIdentity);

            Assert.Multiple(() =>
            {
                Assert.That(sagaDataContainerMetadata[SchemaVersionExtensions.SagaDataContainerSchemaVersionMetadataKey], Is.EqualTo(SagaDataContainer.SchemaVersion));
                Assert.That(sagaUniqueIdentityMetadata[SchemaVersionExtensions.SagaUniqueIdentitySchemaVersionMetadataKey], Is.EqualTo(SagaUniqueIdentity.SchemaVersion));
            });
        }
    }

    class SomeSaga : Saga<SagaData>, IAmStartedByMessages<StartSaga>
    {
        public Task Handle(StartSaga message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.MapSaga(s => s.UniqueString).ToMessage<StartSaga>(m => m.UniqueString);
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
