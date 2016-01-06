using Raven.Client;

namespace NServiceBus.RavenDB.Persistence
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
                throw new Exception("Could not retrieve a RavenDB session. Please ensure that you are using a saga or have enabled outbox.");
            }
            return session;
        }
    }
}
