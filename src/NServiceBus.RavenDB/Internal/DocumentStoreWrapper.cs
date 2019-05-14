namespace NServiceBus.Persistence.RavenDB
{
    using Raven.Client.Documents;

    class DocumentStoreWrapper : IDocumentStoreWrapper
    {
        public DocumentStoreWrapper(IDocumentStore documentStore)
        {
            DocumentStore = documentStore;
        }

        public IDocumentStore DocumentStore { get; }
    }
}