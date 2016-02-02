
namespace NServiceBus
{
    using System;
    using NServiceBus.Extensibility;
    using Raven.Client;

    /// <summary>
    /// Extensions to get the registered RavenDB session
    /// </summary>
    public static class RavenSessionExtension
    {
        /// <summary>
        /// Gets the current RavenDB session.
        /// </summary>
        /// <param name="context">The message handler context.</param>
        public static IAsyncDocumentSession GetRavenSession(this IMessageHandlerContext context)
        {
            return context.Extensions.GetRavenSession();
        }

        /// <summary>
        /// Gets the current RavenDB session.
        /// </summary>
        /// <param name="contextBag">The context bag.</param>
        public static IAsyncDocumentSession GetRavenSession(this ReadOnlyContextBag contextBag)
        {
            Func<IAsyncDocumentSession> sessionFunction;
            contextBag.TryGet(out sessionFunction);
            if (sessionFunction != null)
            {
                return sessionFunction();
            }
            IAsyncDocumentSession session;
            contextBag.TryGet(out session);
            if (session == null)
            {
                throw new Exception(
                    @"GetRavenSession() allows retrieval of the shared Raven IAsyncDocumentSession 
being used by NServiceBus so that additional Raven operations can be completed in the same transactional context as NServiceBus Sagas and Outbox features. 
Because the Saga and Outbox features are not currently in use, it was not possible to retrieve a RavenDB session.");
            }
            return session;
        }
    }
}
