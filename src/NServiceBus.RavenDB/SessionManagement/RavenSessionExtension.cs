﻿namespace NServiceBus
{
    using System;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Testing;
    using Raven.Client;

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
        public static IAsyncDocumentSession RavenSession(this SynchronizedStorageSession session)
        {
            var synchronizedStorageSession = session as RavenDBSynchronizedStorageSession;
            if (synchronizedStorageSession != null)
            {
                return synchronizedStorageSession.Session;
            }

            var testableStorageSession = session as TestableRavenStorageSession;
            if (testableStorageSession != null)
            {
                return testableStorageSession.Session;
            }

            throw new InvalidOperationException("It was not possible to retrieve a RavenDB session.");
        }
    }
}
