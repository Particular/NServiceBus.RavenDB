namespace NServiceBus.Persistence.RavenDB
{
    using Raven.Client;

    /// <summary>
    ///     Provides access the the session managed by NServiceBus
    /// </summary>
    public interface IAsyncSessionProvider
    {
        /// <summary>
        ///     The async session
        /// </summary>
        IAsyncDocumentSession AsyncSession { get; }
    }
}