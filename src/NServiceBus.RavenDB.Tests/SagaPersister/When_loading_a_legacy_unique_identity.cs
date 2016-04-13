using System;
using System.Collections.Generic;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Json.Linq;

[TestFixture]
class When_loading_a_saga_with_legacy_unique_identity : RavenDBPersistenceTestBase
{
    [Test]
    public void It_should_load_successfully()
    {
        var unique = Guid.NewGuid().ToString();

        CreateLegacySagaDocuments(store, unique);

        var factory = new RavenSessionFactory(store);
        factory.ReleaseSession();
        var persister = new SagaPersister(factory);

        var saga = persister.Get<SagaWithUniqueProperty>("UniqueString", unique);

        Assert.IsNotNull(saga, "Saga is null");

        persister.Complete(saga);
        factory.SaveChanges();

        Assert.IsNull(persister.Get<SagaWithUniqueProperty>("UniqueString", unique), "Saga was not completed");
    }

    class SagaWithUniqueProperty : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        [Unique]
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
            Id = SagaUniqueIdentity.FormatId(typeof(SagaWithUniqueProperty), new KeyValuePair<string, object>("UniqueString", unique)),
            SagaId = sagaId,
            SagaDocId = sagaDocId,
            UniqueValue = unique
        };

        DirectStore(store, uniqueIdentity.Id, uniqueIdentity, "SagaUniqueIdentity", "NServiceBus.Persistence.Raven.SagaPersister.SagaUniqueIdentity, NServiceBus.Core");
    }

    static void DirectStore(IDocumentStore store, string id, object document, string entityName, string typeName, string uniqueValue = null)
    {
        var jsonDoc = RavenJObject.FromObject(document);
        var metadata = new RavenJObject();
        metadata[Constants.RavenEntityName] = entityName;
        metadata[Constants.RavenClrType] = typeName;
        if (uniqueValue != null)
            metadata["NServiceBus-UniqueValue"] = uniqueValue;
        Console.WriteLine($"Creating {entityName}: {id}");
        store.DatabaseCommands.Put(id, Etag.Empty, jsonDoc, metadata);
    }
}
