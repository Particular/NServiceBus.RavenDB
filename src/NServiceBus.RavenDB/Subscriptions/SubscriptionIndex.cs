namespace NServiceBus.Persistence.RavenDB
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using Raven.Abstractions.Indexing;
    using Raven.Client;

    static class SubscriptionIndex
    {
        public static void Create(IDocumentStore store)
        {
            new IndexCreator(store).Create();
        }

        public static Task CreateAsync(IDocumentStore store)
        {
            return new IndexCreator(store).CreateAsync();
        }

        class IndexCreator
        {
            IDocumentStore store;
            string indexName;
            IndexDefinition indexDef;

            public IndexCreator(IDocumentStore store)
            {
                this.store = store;
                var collectionName = store.Conventions.FindTypeTagName(typeof(Subscription));
                indexName = $"{collectionName}Index";

                indexDef = new IndexDefinition();
                indexDef.DisableInMemoryIndexing = false;
                indexDef.Fields = new List<string> { "MessageType" };
                //indexDef.IndexVersion = 0; // Not available until Raven 3.5
                indexDef.Map = $"from doc in docs.{collectionName} select new {{ MessageType = doc.MessageType }}";
            }

            public void Create()
            {
                // RavenDB will turn an index PUT into a noop if the index exists and the definitions match
                store.DatabaseCommands.PutIndex(indexName, indexDef, true);
            }

            public Task CreateAsync()
            {
                // RavenDB will turn an index PUT into a noop if the index exists and the definitions match
                return store.AsyncDatabaseCommands.PutIndexAsync(indexName, indexDef, true);
            }
        }
    }
}