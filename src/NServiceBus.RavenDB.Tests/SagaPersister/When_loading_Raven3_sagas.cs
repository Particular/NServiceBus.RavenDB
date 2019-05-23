using System;
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
class Raven3Sagas : RavenDBPersistenceTestBase
{
    protected override void CustomizeDocumentStore(IDocumentStore docStore)
    {
        UnwrappedSagaListener.Register(docStore as DocumentStore);
    }

    [Test]
    public async Task CanLoad()
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

        var dataByCorrelation = await GetSagaDataByProperty();
        var dataById = await GetSagaDataById(sagaId);

        Assert.IsNotNull(dataByCorrelation);
        Assert.IsNotNull(dataById);

        Assert.AreEqual(42, dataByCorrelation.Counter);
        Assert.AreEqual("Alpha", dataByCorrelation.Name);
        Assert.AreEqual(42, dataById.Counter);
        Assert.AreEqual("Alpha", dataById.Name);

    }

    Task<CountingSagaData> GetSagaDataByProperty()
    {
        using (var session = this.OpenAsyncSession())
        {
            var persister = new SagaPersister();
            var context = new ContextBag();
            context.Set(session);
            var synchronizedSession = new RavenDBSynchronizedStorageSession(session);
            return persister.Get<CountingSagaData>("Name", "Alpha", synchronizedSession, context);
        }
    }

    Task<CountingSagaData> GetSagaDataById(Guid sagaId)
    {
        using (var session = this.OpenAsyncSession())
        {
            var persister = new SagaPersister();
            var context = new ContextBag();
            context.Set(session);
            var synchronizedSession = new RavenDBSynchronizedStorageSession(session);
            return persister.Get<CountingSagaData>(sagaId, synchronizedSession, context);
        }
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
            this.Data.Counter++;
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

    static void StoreSaga(IDocumentStore store, string id, object document, string entityName, string typeName, string uniqueValue)
    {
        var documentInfo = new DocumentInfo
        {
            Collection = entityName,
            MetadataInstance = new MetadataAsDictionary()
        };

        documentInfo.MetadataInstance[Constants.Documents.Metadata.RavenClrType] = typeName;
        documentInfo.MetadataInstance["NServiceBus-UniqueValue"] = uniqueValue;

        Console.WriteLine($"Creating {entityName}: {id}");
        using (var session = store.OpenSession())
        {
            var blittableDoc = session.Advanced.EntityToBlittable.ConvertEntityToBlittable(document, documentInfo);
            var command = new PutDocumentCommand(id, string.Empty, blittableDoc);
            session.Advanced.RequestExecutor.Execute(command, session.Advanced.Context);
            session.SaveChanges();
        }
    }

    static void StoreUniqueDoc(IDocumentStore store, string id, SagaUniqueIdentity document)
    {
        var documentInfo = new DocumentInfo
        {
            Collection = "SagaUniqueIdentities",
            MetadataInstance = new MetadataAsDictionary()
        };

        documentInfo.MetadataInstance[Constants.Documents.Metadata.RavenClrType] = "NServiceBus.RavenDB.Persistence.SagaPersister.SagaUniqueIdentity, NServiceBus.RavenDB";

        Console.WriteLine($"Creating unique identity: {id}");
        using (var session = store.OpenSession())
        {
            var blittableDoc = session.Advanced.EntityToBlittable.ConvertEntityToBlittable(document, documentInfo);
            var command = new PutDocumentCommand(id, string.Empty, blittableDoc);
            session.Advanced.RequestExecutor.Execute(command, session.Advanced.Context);
            session.SaveChanges();
        }
    }
}

