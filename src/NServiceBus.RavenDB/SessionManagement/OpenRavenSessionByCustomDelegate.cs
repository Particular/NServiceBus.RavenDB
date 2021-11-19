namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Raven.Client.Documents.Session;

    class OpenRavenSessionByCustomDelegate : IOpenTenantAwareRavenSessions
    {
        public OpenRavenSessionByCustomDelegate(Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSession)
        {
            getAsyncSessionUsingHeaders = getAsyncSession;
        }

        public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders, SessionOptions sessionOptions)
        {
            var session = getAsyncSessionUsingHeaders(messageHeaders);

            session.Advanced.UseOptimisticConcurrency = true;
            // TODO: decide what to do here

            // warn against incompatible settings: cluster-wide tx opt
            if (!session.Advanced.UseOptimisticConcurrency && sessionOptions.TransactionMode != TransactionMode.ClusterWide)
            {
                Logger.Error("Not using cluster-wide transactions and optimistic concurrency is considered unsafe. Are you sure you want to continue in this configuration?");
            }

            return session;
        }

        Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSessionUsingHeaders;
        static readonly ILog Logger = LogManager.GetLogger(typeof(OpenRavenSessionByCustomDelegate));
    }
}