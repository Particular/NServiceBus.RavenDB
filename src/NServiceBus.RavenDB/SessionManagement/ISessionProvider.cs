namespace NServiceBus.RavenDB.Persistence
{
    using Raven.Client;

    interface ISessionProvider
    {
        IDocumentSession Session { get; }
    }
}