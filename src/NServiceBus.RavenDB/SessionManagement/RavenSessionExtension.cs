namespace NServiceBus
{
    using System;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Testing;
    using Raven.Client.Documents.Session;

    /// <summary>
    /// Extensions to manage RavenDB session.
    /// </summary>
    public static class RavenSessionExtension
    {
        /// <summary>
        /// Gets the current RavenDB session.
        /// </summary>
        /// <param name="session">The storage session.</param>
        public static IAsyncDocumentSession RavenSession(this ISynchronizedStorageSession session)
        {
            switch (session)
            {
                case RavenDBSynchronizedStorageSession ISynchronizedStorageSession:
                    return ISynchronizedStorageSession.Session;
                case TestableRavenStorageSession testableStorageSession:
                    return testableStorageSession.Session;
                default:
                    throw new InvalidOperationException("It was not possible to retrieve a RavenDB session.");
            }
        }
    }
}