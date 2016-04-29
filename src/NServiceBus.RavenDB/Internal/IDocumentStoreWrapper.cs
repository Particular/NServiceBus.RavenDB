namespace NServiceBus.Persistence.RavenDB
{
    using Raven.Client;

    interface IDocumentStoreWrapper
    {
        IDocumentStore DocumentStore { get; }
    }
}