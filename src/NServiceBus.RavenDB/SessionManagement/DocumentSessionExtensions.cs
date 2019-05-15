namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using Raven.Client.Documents.Session;

    static class DocumentSessionExtensions
    {
        public static bool IsClusterWideTransaction(this IAsyncDocumentSession session)
        {
            if (session is InMemoryDocumentSessionOperations ops)
            {
                return ops.TransactionMode == TransactionMode.ClusterWide;
            }

            throw new InvalidOperationException("Don't know what the documentSession is, can't infer transaction mode.");
        }
    }
}
