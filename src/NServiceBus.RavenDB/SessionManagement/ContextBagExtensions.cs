namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using NServiceBus.Extensibility;
    using Raven.Client;

    static class ContextBagExtensions
    {
        /// <summary>
        /// Retrieves an IAsyncDocumentSession from the ContextBag. If a sessionFunction exists, that will be used
        /// to create the session. Otherwise, retrieve the session directly from the bag.
        /// </summary>
        internal static SessionOwnership GetSessionOwnership(this ContextBag contextBag)
        {
            Func<IAsyncDocumentSession> sessionFunction;
            contextBag.TryGet(out sessionFunction);
            if (sessionFunction != null)
            {
                return new SessionOwnership(false, sessionFunction());
            }

            IAsyncDocumentSession session;
            if (contextBag.TryGet(out session))
            {
                return new SessionOwnership(true, session);
            }

            throw new Exception("IAsyncDocumentSession could not be retrieved for the incoming message pipeline.");
        }
    }
}