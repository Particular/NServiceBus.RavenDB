namespace NServiceBus.Persistence.RavenDB
{
    using Raven.Client.Documents;

    interface IDocumentStoreWrapper
    {
        IDocumentStore DocumentStore { get; }
    }
}