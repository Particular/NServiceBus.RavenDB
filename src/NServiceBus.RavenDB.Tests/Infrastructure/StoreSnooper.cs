namespace NServiceBus.RavenDB.Tests
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using Raven.Client.Documents;

    class StoreSnooper : IDocumentStoreListener
    {
        public IEnumerable<string> KeysStored => keysStored;
        public int KeyCount => keysStored.Count;

        private StoreSnooper()
        {
        }

        public bool BeforeStore(string key, object entityInstance, JObject metadata, JObject original)
        {
            return true;
        }

        public void AfterStore(string key, object entityInstance, JObject metadata)
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

        private List<string> keysStored = new List<string>(); 
    }
}
