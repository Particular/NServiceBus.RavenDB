namespace NServiceBus.Testing
{
    using Persistence;
    using Raven.Client.Documents.Session;

    /// <summary>
    /// A fake implementation for <see cref="SynchronizedStorageSession" /> for testing purposes.
    /// </summary>
    public class TestableRavenStorageSession : SynchronizedStorageSession
    {
        /// <summary>
        /// Creates a new instance of <see cref="TestableRavenStorageSession" />
        /// using the provided <see cref="IAsyncDocumentSession" />.
        /// </summary>
        /// <param name="session"></param>
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