namespace NServiceBus.Persistence.RavenDB.Subscriptions
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
            var indexDef = new IndexDefinition();
            indexDef.DisableInMemoryIndexing = false;
            indexDef.Fields = new List<string> { "MessageType" };
            //indexDef.IndexVersion = 0;
            indexDef.Map = $"from doc in docs.{collectionName} select new {{ MessageType = doc.MessageType }}";

            await store.AsyncDatabaseCommands.PutIndexAsync($"{collectionName}Index", indexDef).ConfigureAwait(false);
        }
    }
}