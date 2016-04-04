
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
        public static IAsyncDocumentSession RavenSession(this SynchronizedStorageSession session)
        {
            var synchronizedStorageSession = session as RavenDBSynchronizedStorageSession;
            
            if (synchronizedStorageSession == null)
            {
                throw new InvalidOperationException("It was not possible to retrieve a RavenDB session.");
            }

            return synchronizedStorageSession.Session;
        }

        /// <summary>
        /// Sets the current RavenDB session received by <see cref="RavenSession"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="session"></param>
        public static void SetRavenSession(this IMessageHandlerContext context, IAsyncDocumentSession session)
        {
            context.Extensions.Set<SynchronizedStorageSession>(new RavenDBSynchronizedStorageSession(session, true));
        }
    }
}
