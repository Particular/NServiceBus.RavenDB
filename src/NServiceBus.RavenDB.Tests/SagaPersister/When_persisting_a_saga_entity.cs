using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;
using Raven.Client.Documents.Session;

[TestFixture]
public class When_persisting_a_saga_entity : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Schema_version_should_be_persisted()
    {
        var entity = new SagaData
        {
            Id = Guid.NewGuid(),
            UniqueString = "SomeUniqueString",
        };

        IAsyncDocumentSession session;
        var options = this.CreateContextWithAsyncSessionPresent(out session);
        var persister = new SagaPersister();
        var synchronizedSession = new RavenDBSynchronizedStorageSession(session);

        await persister.Save(entity, this.CreateMetadata<SomeSaga>(entity), synchronizedSession, options);
        await session.SaveChangesAsync().ConfigureAwait(false);

        var savedEntity = await persister.Get<SagaData>(entity.Id, synchronizedSession, options);
        var id = SagaPersister.DocumentIdForSagaData(session, savedEntity);
        var storedEntity = await session.LoadAsync<SagaDataContainer>(id);
        var sagaMetadata = session.Advanced.GetMetadataFor(storedEntity);
        var storedUniqueIdEntity = await session.LoadAsync<SagaUniqueIdentity>(storedEntity.IdentityDocId);
        var uniqueIdentityMetadata = session.Advanced.GetMetadataFor(storedUniqueIdEntity);

        Assert.AreEqual(SagaDataContainer.SchemaVersion.ToString(3), sagaMetadata[SessionVersionExtensions.SagaDataVersionMetadataKey]);
        Assert.AreEqual(SagaUniqueIdentity.SchemaVersion.ToString(3), uniqueIdentityMetadata[SessionVersionExtensions.SagaUniqueIdentityVersionMetadataKey]);
    }

    class SomeSaga : Saga<SagaData>, IAmStartedByMessages<StartSaga>
    {
        public Task Handle(StartSaga message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartSaga>(m => m.UniqueString).ToSaga(s => s.UniqueString);
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