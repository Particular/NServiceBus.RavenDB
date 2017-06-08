namespace NServiceBus.RavenDB.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Raven.Client;
    using Raven.Client.Listeners;
    using Raven.Json.Linq;

    class StoreSnooper : IDocumentStoreListener
    {
        public IEnumerable<string> KeysStored => keysStored;
        public int KeyCount => keysStored.Count;

        StoreSnooper()
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

        public void Clear()
        {
            keysStored.Clear();
        }

        public static StoreSnooper Install(IDocumentStore store)
        {
            var snooper = new StoreSnooper();
            var currentListeners = store.Listeners.StoreListeners.ToList();
            currentListeners.Add(snooper);
            store.Listeners.StoreListeners = currentListeners.ToArray();
            return snooper;
        }

        List<string> keysStored = new List<string>(); 
    }
}
