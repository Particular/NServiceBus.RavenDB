namespace NServiceBus.RavenDB.Internal
{
    using Raven.Client;

    public class DocumentStoreWrapper : IDocumentStoreWrapper
    {
        public IDocumentStore DocumentStore { get; set; }
    }

    public interface IDocumentStoreWrapper
    {
        IDocumentStore DocumentStore { get; }
    }
}
