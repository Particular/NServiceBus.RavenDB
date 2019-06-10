namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.Extensibility;
    using Raven.Client.Documents.Session;

    static class ContextBagExtensions
    {
        /// <summary>
        /// Retrieves an IAsyncDocumentSession from the ContextBag. If a sessionFunction exists, that will be used
        /// to create the session. Otherwise, retrieve the session directly from the bag.
        /// </summary>
        internal static IAsyncDocumentSession GetAsyncSession(this ContextBag contextBag)
        {
            Func<IAsyncDocumentSession> sessionFunction;
            contextBag.TryGet(out sessionFunction);
            if (sessionFunction != null)
            {
                return sessionFunction();
            }

            IAsyncDocumentSession session;
            if (contextBag.TryGet(out session))
            {
                return session;
            }

            throw new Exception("IAsyncDocumentSession could not be retrieved for the incoming message pipeline.");
        }
    }
}