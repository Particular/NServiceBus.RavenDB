namespace NServiceBus.RavenDB.Internal
{
    using Raven.Client;

    interface IDocumentStoreWrapper
    {
        IDocumentStore DocumentStore { get; }
    }
}