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
            var txMode = ((InMemoryDocumentSessionOperations)session).TransactionMode;

            // TODO: throw all the exceptions if the config is not aligned with the session settings
            if (txMode == TransactionMode.ClusterWide)
            {
                if (session.Advanced.UseOptimisticConcurrency)
                {
                    throw new InvalidOperationException("The configured session is set to use Cluster wide transactions, but the session's concurrency behavior is set to optimistic concurrency, which is incompatible.");
                }
            }
            else
            {
                // Optimistic concurrency is not compatible with cluster wide concurrency
                session.Advanced.UseOptimisticConcurrency = true;
            }


            return session;
        }

        Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSessionUsingHeaders;
    }
}