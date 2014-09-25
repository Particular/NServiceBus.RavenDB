namespace NServiceBus.RavenDB.Persistence
{
    using Raven.Client;

    /// <summary>
    /// Provides access the the session managed by NServiceBus
    /// </summary>
    public interface ISessionProvider
    {

        /// <summary>
        /// The session
        /// </summary>
        IDocumentSession Session { get; }
    }
}