namespace NServiceBus.RavenDB.Internal
{
    using Raven.Client;

    class DocumentStoreWrapper : IDocumentStoreWrapper
    {
        readonly IDocumentStore documentStore;

        public DocumentStoreWrapper(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public IDocumentStore DocumentStore
        {
            get { return documentStore; }
        }
    }

    interface IDocumentStoreWrapper
    {
        IDocumentStore DocumentStore { get; }
    }
}