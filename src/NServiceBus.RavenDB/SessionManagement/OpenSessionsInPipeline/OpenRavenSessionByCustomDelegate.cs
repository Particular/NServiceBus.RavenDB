namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Raven.Client;

    class OpenRavenSessionByCustomDelegate : IOpenRavenSessionsInPipeline
    {
        Func<IAsyncDocumentSession> getAsyncSession;
        Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSessionUsingHeaders;

        public OpenRavenSessionByCustomDelegate(Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSession)
        {
            this.getAsyncSessionUsingHeaders = getAsyncSession;
        }

        [ObsoleteEx(RemoveInVersion = "6.0.0")]
        public OpenRavenSessionByCustomDelegate(Func<IAsyncDocumentSession> getAsyncSession)
        {
            this.getAsyncSession = getAsyncSession;
        }

        public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders)
        {
            if (getAsyncSessionUsingHeaders != null)
            {
                return getAsyncSessionUsingHeaders(messageHeaders);
            }
            return getAsyncSession();
        }
    }
}