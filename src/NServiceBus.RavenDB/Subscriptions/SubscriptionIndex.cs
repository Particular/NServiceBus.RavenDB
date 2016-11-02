namespace NServiceBus.Persistence.RavenDB
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using Raven.Abstractions.Indexing;
    using Raven.Client;

    static class SubscriptionIndex
    {
        public static async Task Create(IDocumentStore store)
        {
            var collectionName = store.Conventions.FindTypeTagName(typeof(Subscription));
            var indexName = $"{collectionName}Index";

            var indexDef = new IndexDefinition();
            indexDef.DisableInMemoryIndexing = false;
            indexDef.Fields = new List<string> { "MessageType" };
            //indexDef.IndexVersion = 0; // Not available until Raven 3.5
            indexDef.Map = $"from doc in docs.{collectionName} select new {{ MessageType = doc.MessageType }}";

            // RavenDB will turn an index PUT into a noop if the index exists and the definitions match
            await store.AsyncDatabaseCommands.PutIndexAsync(indexName, indexDef, true).ConfigureAwait(false);
        }
    }
}