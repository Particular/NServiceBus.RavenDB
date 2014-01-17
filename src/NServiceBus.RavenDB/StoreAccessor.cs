namespace NServiceBus.RavenDB.Persistence
{
    using System;
    using Raven.Client;

    class StoreAccessor : IDisposable
    {
        IDocumentStore store;

        public StoreAccessor(IDocumentStore store)
        {
            this.store = store;
        }

        public IDocumentStore Store
        {
            get
            {
                return store;
            }
        }

        public void Dispose()
        {
            //Injected at compile time
        }
    }
}