using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Session;
using Raven.Client.Json;

[TestFixture]
class When_loading_a_saga_with_legacy_unique_identity : RavenDBPersistenceTestBase
{
    protected override void CustomizeDocumentStore(IDocumentStore store)
    {
        UnwrappedSagaListener.Register(store as DocumentStore);
    }

    [Test]
    public async Task It_should_load_successfully()
    {
        var unique = Guid.NewGuid().ToString();

        CreateLegacySagaDocuments(store, unique);

        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency().InContext(out var options))
        {
            var persister = new SagaPersister(new SagaPersistenceConfiguration());

            var synchronizedSession = new RavenDBSynchronizedStorageSession(session, options);

            var saga = await persister.Get<SagaWithUniqueProperty>("UniqueString", unique, synchronizedSession, options);

            Assert.IsNotNull(saga, "Saga is null");
            Assert.AreNotEqual(Guid.Empty, saga.Id, "Id is Guid.Empty");

            await persister.Complete(saga, synchronizedSession, options);
            await session.SaveChangesAsync().ConfigureAwait(false);

            Assert.IsNull(await persister.Get<SagaWithUniqueProperty>("UniqueString", unique, synchronizedSession, options), "Saga was not completed");
        }
    }

    [Test]
    public async Task Improperly_converted_saga_can_be_fixed()
    {
        var sagaId = Guid.NewGuid();
        var sagaDocId = $"SagaWithUniqueProperty/{sagaId}";
        var uniqueString = "abcd";
        var identityDocId = SagaUniqueIdentity.FormatId(typeof(SagaWithUniqueProperty), "UniqueString", uniqueString);

        var sagaData = new SagaWithUniqueProperty
        {
            Id = Guid.Empty, // Improperly converted
            UniqueString = uniqueString
        };

        var sagaContainer = new SagaDataContainer
        {
            Id = sagaDocId,
            Data = sagaData,
            IdentityDocId = identityDocId
        };

        var uniqueIdentity = new SagaUniqueIdentity
        {
            Id = SagaUniqueIdentity.FormatId(typeof(SagaWithUniqueProperty), "UniqueString", uniqueString),
            SagaId = sagaId,
            SagaDocId = sagaDocId,
            UniqueValue = uniqueString
        };

        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency().InContext(out var _))
        {
            await session.StoreAsync(sagaContainer);
            await session.StoreAsync(uniqueIdentity);
            await session.SaveChangesAsync();
        }

        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency().InContext(out var options))
        {
            var persister = new SagaPersister(new SagaPersistenceConfiguration());

            var synchronizedSession = new RavenDBSynchronizedStorageSession(session, options);

            var loadedSaga = await persister.Get<SagaWithUniqueProperty>("UniqueString", uniqueString, synchronizedSession, options);

            Assert.IsNotNull(loadedSaga, "Saga is null");
            Assert.AreNotEqual(Guid.Empty, loadedSaga.Id, "Id is Guid.Empty");
            Assert.AreEqual(sagaId, loadedSaga.Id, "Saga Id is not the correct value.");
        }
    }

    class SagaWithUniqueProperty : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public string UniqueString { get; set; }
    }

    static void CreateLegacySagaDocuments(IDocumentStore store, string unique)
    {
        var sagaId = Guid.NewGuid();

        var saga = new SagaWithUniqueProperty
        {
            Id = sagaId,
            UniqueString = unique
        };

        var sagaDocId = $"SagaWithUniqueProperty/{sagaId}";
        var typeName = Regex.Replace(typeof(SagaWithUniqueProperty).AssemblyQualifiedName, ", Version=.*", "");

        DirectStore(store, sagaDocId, saga, "SagaWithUniqueProperty", typeName, unique);

        var uniqueIdentity = new SagaUniqueIdentity
        {
            Id = SagaUniqueIdentity.FormatId(typeof(SagaWithUniqueProperty), "UniqueString", unique),
            SagaId = sagaId,
            SagaDocId = sagaDocId,
            UniqueValue = unique
        };

        DirectStore(store, uniqueIdentity.Id, uniqueIdentity, "SagaUniqueIdentity", "NServiceBus.Persistence.Raven.SagaPersister.SagaUniqueIdentity, NServiceBus.Core");
    }

    static void DirectStore(IDocumentStore store, string id, object document, string entityName, string typeName, string uniqueValue = null)
    {
        var documentInfo = new DocumentInfo
        {
            Collection = entityName,
            MetadataInstance = new MetadataAsDictionary()
        };

        documentInfo.MetadataInstance[Constants.Documents.Metadata.RavenClrType] = typeName;
        if (uniqueValue != null)
        {
            documentInfo.MetadataInstance["NServiceBus-UniqueValue"] = uniqueValue;
        }

        Console.WriteLine($"Creating {entityName}: {id}");
        using (var session = store.OpenSession())
        {
            var blittableDoc = session.Advanced.JsonConverter.ToBlittable(document, documentInfo);
            var command = new PutDocumentCommand(id, string.Empty, blittableDoc);
            session.Advanced.RequestExecutor.Execute(command, session.Advanced.Context);
            session.SaveChanges();
        }


    }
}
