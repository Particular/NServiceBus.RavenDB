namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Logging;
    using Raven.Client.Documents.Session;

    class OpenRavenSessionByCustomDelegate : IOpenTenantAwareRavenSessions
    {
        public OpenRavenSessionByCustomDelegate(Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSession, bool useClusterWideTx)
        {
            getAsyncSessionUsingHeaders = getAsyncSession;
            this.useClusterWideTx = useClusterWideTx;
        }

        public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders)
        {
            var session = getAsyncSessionUsingHeaders(messageHeaders);
            var txMode = ((InMemoryDocumentSessionOperations)session).TransactionMode;

            if (!useClusterWideTx && txMode == TransactionMode.ClusterWide)
            {
                throw new Exception("To use cluster-wide transactions enable support via the UseClusterWideTransactions() RavenDB Persistence configuration option.");
            }

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
                if (!session.Advanced.UseOptimisticConcurrency)
                {
                    logger.Info("RavenDB persistence requires UseOptimisticConcurrency to be set to true. Current value if false, setting it to true.");
                    session.Advanced.UseOptimisticConcurrency = true;
                }
            }


            return session;
        }

        Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSessionUsingHeaders;
        readonly bool useClusterWideTx;
        static readonly ILog logger = LogManager.GetLogger<OpenRavenSessionByCustomDelegate>();
    }
}