namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System.Collections.Generic;
    using System.Linq;
    using Raven.Client;
    using Raven.Client.Listeners;
    using Raven.Json.Linq;

    class StoreSnooper : IDocumentStoreListener
    {
        public IEnumerable<string> KeysStored => keysStored;

        private StoreSnooper()
        {
        }

        public bool BeforeStore(string key, object entityInstance, RavenJObject metadata, RavenJObject original)
        {
            return true;
        }

        public void AfterStore(string key, object entityInstance, RavenJObject metadata)
        {
            keysStored.Add(key);
        }

        public static StoreSnooper Install(IDocumentStore store)
        {
            var snooper = new StoreSnooper();
            var currentListeners = store.Listeners.StoreListeners.ToList();
            currentListeners.Add(snooper);
            store.Listeners.StoreListeners = currentListeners.ToArray();
            return snooper;
        }

        private List<string> keysStored = new List<string>(); 
    }
}
