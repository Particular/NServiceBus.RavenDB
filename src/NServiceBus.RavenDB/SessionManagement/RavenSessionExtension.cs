
namespace NServiceBus
{
    using System;
    using NServiceBus.Persistence;
    using NServiceBus.SagaPersisters.RavenDB;
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
        public static IAsyncDocumentSession Session(this SynchronizedStorageSession session)
        {
            var synchronizedStorageSession = session as RavenDBSynchronizedStorageSession;
            
            if (synchronizedStorageSession == null)
            {
                throw new InvalidOperationException("Shared session has not been configured for RavenDB, this is usually because the Saga and Outbox features are not currently in use, it was not possible to retrieve a RavenDB session.");
            }

            return synchronizedStorageSession.Session;
        }
    }
}
