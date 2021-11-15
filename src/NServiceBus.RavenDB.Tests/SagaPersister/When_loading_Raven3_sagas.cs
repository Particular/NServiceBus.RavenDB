using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
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
// This class name is important - it's simple to make it easier to insert fake Raven3.x-like saga data
class Raven3Sagas : RavenDBPersistenceTestBase
{
    protected override void CustomizeDocumentStore(IDocumentStore docStore)
    {
        UnwrappedSagaListener.Register(docStore as DocumentStore);
    }

    [Test]
    public Task CanLoadAndRemoveByCorrelation()
    {
        return RunTestUsing((persister, sagaId, syncSession, context, cancellationToken) => persister.Get<CountingSagaData>("Name", "Alpha", syncSession, context, cancellationToken));
    }

    [Test]
    public Task CanLoadAndRemoveById()
    {
        return RunTestUsing((persister, sagaId, syncSession, context, cancellationToken) => persister.Get<CountingSagaData>(sagaId, syncSession, context, cancellationToken));
    }

    async Task RunTestUsing(GetSagaDelegate getSaga, CancellationToken cancellationToken = default)
    {
        var sagaId = StoreSagaDocuments();

        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency())
        {
            var persister = new SagaPersister(new SagaPersistenceConfiguration());
            var context = new ContextBag();
            context.Set(session);
            var synchronizedSession = new RavenDBSynchronizedStorageSession(session, context);
            var sagaData = await getSaga(persister, sagaId, synchronizedSession, context, cancellationToken);

            Assert.IsNotNull(sagaData);
            Assert.AreEqual(42, sagaData.Counter);
            Assert.AreEqual("Alpha", sagaData.Name);

            await persister.Complete(sagaData, synchronizedSession, context, cancellationToken);
            await session.SaveChangesAsync(cancellationToken);
        }

        using (var session = store.OpenAsyncSession().UsingOptimisticConcurrency())
        {
            var dataDocs = await session.Advanced.LoadStartingWithAsync<SagaDataContainer>("CountingSagaDatas/", token: cancellationToken);
            var uniqueDocs = await session.Advanced.LoadStartingWithAsync<SagaUniqueIdentity>("Raven3Sagas-", token: cancellationToken);

            Assert.IsEmpty(dataDocs);
            Assert.IsEmpty(uniqueDocs);
        }
    }

    delegate Task<CountingSagaData> GetSagaDelegate(SagaPersister persister, Guid sagaId, RavenDBSynchronizedStorageSession syncSession, ContextBag context, CancellationToken cancellationToken = default);

    Guid StoreSagaDocuments()
    {
        var sagaId = Guid.NewGuid();
        var sagaDocId = $"CountingSagaDatas/{sagaId}";
        var uniqueDocId = "Raven3Sagas-CountingSagaData/Name/5f293261-55cf-fb70-8b0a-944ef322a598"; // Guid is hash of "Alpha"
        var typeName = "Raven3Sagas+CountingSagaData, NServiceBus.RavenDB.Tests";

        var oldData = new CountingSagaData
        {
            Name = "Alpha",
            Id = sagaId,
            Counter = 42,
            Originator = "DoesntMatter@MACHINE",
            OriginalMessageId = Guid.NewGuid().ToString()
        };

        var uniqueDoc = new SagaUniqueIdentity
        {
            Id = uniqueDocId,
            SagaDocId = sagaDocId,
            SagaId = sagaId,
            UniqueValue = "Alpha"
        };

        StoreSaga(store, sagaDocId, oldData, "CountingSagaDatas", typeName, uniqueDocId);
        StoreUniqueDoc(store, uniqueDocId, uniqueDoc);
        return sagaId;
    }

    public class CountingSaga : Saga<CountingSagaData>,
        IAmStartedByMessages<CountMsg>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CountingSagaData> mapper)
        {
            mapper.ConfigureMapping<CountMsg>(m => m.Name).ToSaga(s => s.Name);
        }

        public Task Handle(CountMsg message, IMessageHandlerContext context)
        {
            Data.Counter++;
            return Task.CompletedTask;
        }
    }

    public class CountingSagaData : ContainSagaData
    {
        public string Name { get; set; }
        public int Counter { get; set; }
    }

    public class CountMsg : ICommand
    {
        public string Name { get; set; }
    }

    static void StoreSaga(IDocumentStore store, string id, object document, string entityName, string typeName, string uniqueDocId)
    {
        var documentInfo = new DocumentInfo
        {
            MetadataInstance = new MetadataAsDictionary()
        };

        documentInfo.MetadataInstance[Constants.Documents.Metadata.RavenClrType] = typeName;
        documentInfo.MetadataInstance["NServiceBus-UniqueDocId"] = uniqueDocId;
        documentInfo.MetadataInstance[Constants.Documents.Metadata.Collection] = entityName;

        Console.WriteLine($"Creating {entityName}: {id}");
        using (var session = store.OpenSession())
        {
            var blittableDoc = session.Advanced.JsonConverter.ToBlittable(document, documentInfo);
            var command = new PutDocumentCommand(id, string.Empty, blittableDoc);
            session.Advanced.RequestExecutor.Execute(command, session.Advanced.Context);
            session.SaveChanges();
        }
    }

    static void StoreUniqueDoc(IDocumentStore store, string id, SagaUniqueIdentity document)
    {
        var documentInfo = new DocumentInfo
        {
            MetadataInstance = new MetadataAsDictionary()
        };

        documentInfo.MetadataInstance[Constants.Documents.Metadata.RavenClrType] = "NServiceBus.RavenDB.Persistence.SagaPersister.SagaUniqueIdentity, NServiceBus.RavenDB";
        documentInfo.MetadataInstance[Constants.Documents.Metadata.Collection] = "SagaUniqueIdentities";

        Console.WriteLine($"Creating unique identity: {id}");
        using (var session = store.OpenSession())
        {
            var blittableDoc = session.Advanced.JsonConverter.ToBlittable(document, documentInfo);
            var command = new PutDocumentCommand(id, string.Empty, blittableDoc);
            session.Advanced.RequestExecutor.Execute(command, session.Advanced.Context);
            session.SaveChanges();
        }
    }
}