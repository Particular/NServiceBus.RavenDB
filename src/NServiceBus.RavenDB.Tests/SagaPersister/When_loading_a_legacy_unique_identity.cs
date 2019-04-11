using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

[TestFixture]
class When_loading_a_saga_with_legacy_unique_identity : RavenDBPersistenceTestBase
{
    [Test]
    public async Task It_should_load_successfully()
    {
        var unique = Guid.NewGuid().ToString();

        CreateLegacySagaDocuments(store, unique);

        IAsyncDocumentSession session;
        var options = this.CreateContextWithAsyncSessionPresent(out session);
        var persister = new SagaPersister();

        var synchronizedSession = new RavenDBSynchronizedStorageSession(session, true);

        var saga = await persister.Get<SagaWithUniqueProperty>("UniqueString", unique, synchronizedSession, options);

        Assert.IsNotNull(saga, "Saga is null");

        await persister.Complete(saga, synchronizedSession, options);
        await session.SaveChangesAsync().ConfigureAwait(false);

        Assert.IsNull(await persister.Get<SagaWithUniqueProperty>("UniqueString", unique, synchronizedSession, options), "Saga was not completed");
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

        DirectStore(store, sagaDocId, saga, "SagaWithUniqueProperty", ReflectionUtil.GetFullNameWithoutVersionInformation(typeof(SagaWithUniqueProperty)), unique);

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
        var jsonDoc = JObject.FromObject(document);
        var metadata = new JObject();
        metadata[Constants.RavenEntityName] = entityName;
        metadata[Constants.RavenClrType] = typeName;
        if (uniqueValue != null)
            metadata["NServiceBus-UniqueValue"] = uniqueValue;
        Console.WriteLine($"Creating {entityName}: {id}");
        store.DatabaseCommands.Put(id, Etag.Empty, jsonDoc, metadata);
    }
}
