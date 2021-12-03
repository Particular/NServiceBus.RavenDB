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
        /// <returns></returns>
        public static IAsyncDocumentSession RavenSession(this SynchronizedStorageSession session) => session switch
        {
            RavenDBSynchronizedStorageSession synchronizedStorageSession => synchronizedStorageSession.Session,
            TestableRavenStorageSession testableStorageSession => testableStorageSession.Session,
            _ => throw new InvalidOperationException("It was not possible to retrieve a RavenDB session.")
        };
    }
}