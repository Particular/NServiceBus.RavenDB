using System;
using System.Collections.Generic;
using NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister;
using NServiceBus.RavenDB;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NUnit.Framework;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Json.Linq;

[TestFixture]
class When_loading_a_saga_with_legacy_unique_identity : Raven_saga_persistence_concern
{
    [Test]
    public void It_should_load_successfully()
    {
        var unique = Guid.NewGuid().ToString();
        CreateLegacySagaDocuments(unique);

        store.Conventions.FindClrType = ConfigureRavenPersistence.FindClrType;

        WithASagaPersistenceUnitOfWork(p =>
        {
            var uniqueSaga = p.Get<SagaWithUniqueProperty>("UniqueString", unique);

            Assert.IsNotNull(uniqueSaga, "Saga was not found.");

            p.Complete(uniqueSaga);

            Assert.IsNull(p.Get<SagaWithUniqueProperty>("UniqueString", unique), "Saga was not completed.");
        });
    }

    void CreateLegacySagaDocuments(string unique)
    {
        var sagaId = Guid.NewGuid();

        var saga = new SagaWithUniqueProperty
        {
            Id = sagaId,
            UniqueString = unique
        };

        var sagaDocId = $"SagaWithUniqueProperties/{sagaId}";

        DirectStore(store, sagaDocId, saga, "SagaWithUniqueProperties", "NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister.SagaWithUniqueProperty, NServiceBus.RavenDB.Tests", unique);

        var uniqueIdentity = new SagaUniqueIdentity
        {
            Id = SagaUniqueIdentity.FormatId(typeof(SagaWithUniqueProperty), new KeyValuePair<string, object>("UniqueString", unique)),
            SagaId = sagaId,
            SagaDocId = sagaDocId,
            UniqueValue = unique
        };

        DirectStore(store, uniqueIdentity.Id, uniqueIdentity, "SagaUniqueIdentities", "NServiceBus.Persistence.Raven.SagaPersister.SagaUniqueIdentity, NServiceBus.Core");
    }

    static void DirectStore(IDocumentStore store, string id, object document, string entityName, string typeName, string uniqueValue = null)
    {
        var jsonDoc = RavenJObject.FromObject(document);
        var metadata = new RavenJObject();
        metadata[Constants.RavenEntityName] = entityName;
        metadata[Constants.RavenClrType] = typeName;
        if(uniqueValue != null)
            metadata["NServiceBus-UniqueValue"] = uniqueValue;
        Console.WriteLine($"Creating {entityName}: {id}");
        store.DatabaseCommands.Put(id, Etag.Empty, jsonDoc, metadata);
    }
}
