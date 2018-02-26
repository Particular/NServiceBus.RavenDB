namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Raven.Client;

    class OpenRavenSessionByCustomDelegate : IOpenRavenSessionsInPipeline
    {
        Func<IDocumentSession> getSession;
        Func<IDictionary<string, string>, IDocumentSession> getSessionUsingHeaders;

        public OpenRavenSessionByCustomDelegate(Func<IDictionary<string, string>, IDocumentSession> getSessionUsingHeaders)
        {
            this.getSessionUsingHeaders = getSessionUsingHeaders;
        }

        //TODO: [ObsoleteEx(RemoveInVersion = "6.0.0")]
        public OpenRavenSessionByCustomDelegate(Func<IDocumentSession> getSession)
        {
            this.getSession = getSession;
        }

        public IDocumentSession OpenSession(IDictionary<string, string> headers)
        {
            if (getSessionUsingHeaders != null)
            {
                return getSessionUsingHeaders(headers);
            }
            return getSession();
        }
    }
}