namespace NServiceBus.RavenDB.Tests
{
    using System.Collections.Generic;
    using Raven.Client.Documents;

    class StoreSnooper
    {
        public IEnumerable<string> KeysStored => keysStored;
        public int KeyCount => keysStored.Count;

        private StoreSnooper()
        {
        }

        public void Clear()
        {
            keysStored.Clear();
        }

        public static StoreSnooper Install(DocumentStore store)
        {
            var snooper = new StoreSnooper();

            store.OnBeforeStore += (sender, args) => snooper.keysStored.Add(args.DocumentId);

            return snooper;
        }

        private List<string> keysStored = new List<string>();
    }
}
