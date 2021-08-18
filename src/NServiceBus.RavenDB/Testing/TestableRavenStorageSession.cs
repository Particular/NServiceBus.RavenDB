namespace NServiceBus.Testing
{
    using NServiceBus.Persistence;
    using Raven.Client.Documents.Session;

    /// <summary>
    /// A fake implementation for <see cref="ISynchronizedStorageSession" /> for testing purposes.
    /// </summary>
    public class TestableRavenStorageSession : ISynchronizedStorageSession
    {
        /// <summary>
        /// Creates a new instance of <see cref="TestableRavenStorageSession" />
        /// using the provided <see cref="IAsyncDocumentSession" />.
        /// </summary>
        public TestableRavenStorageSession(IAsyncDocumentSession session)
        {
            Session = session;
        }

        /// <summary>
        /// The document session which is retrieved by calling <see cref="RavenSessionExtension.RavenSession" />.
        /// </summary>
        public IAsyncDocumentSession Session { get; }
    }
}