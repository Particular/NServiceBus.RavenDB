using Raven.Client;

namespace NServiceBus
{
    using System;

    /// <summary>
    /// Extensions, to the message handler context, to manage RavenDB session.
    /// </summary>
    public static class RavenSessionExtension
    {
        /// <summary>
        /// Gets the current RavenDB session.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static IAsyncDocumentSession GetRavenSession(this IMessageHandlerContext context)
        {
            Func<IAsyncDocumentSession> sessionFunction;
            context.Extensions.TryGet(out sessionFunction);
            if (sessionFunction != null)
            {
                return sessionFunction();
            }
            IAsyncDocumentSession session;
            context.Extensions.TryGet(out session);
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
