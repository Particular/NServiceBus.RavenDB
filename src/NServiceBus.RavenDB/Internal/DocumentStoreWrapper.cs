namespace NServiceBus.RavenDB.Internal
{
    using Raven.Client;

    class DocumentStoreWrapper : IDocumentStoreWrapper
    {
        public IDocumentStore DocumentStore { get; set; }
    }

    interface IDocumentStoreWrapper
    {
        IDocumentStore DocumentStore { get; }
    }
}
