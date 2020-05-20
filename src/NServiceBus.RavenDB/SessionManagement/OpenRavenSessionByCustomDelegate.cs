namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Raven.Client.Documents.Session;

    class OpenRavenSessionByCustomDelegate : IOpenTenantAwareRavenSessions
    {
        public OpenRavenSessionByCustomDelegate(Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSession)
        {
            getAsyncSessionUsingHeaders = getAsyncSession;
        }

        public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders)
        {
            var session = getAsyncSessionUsingHeaders(messageHeaders);

            //session.Advanced.UseOptimisticConcurrency = true;

            return session;
        }

        Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSessionUsingHeaders;
    }
}