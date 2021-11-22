namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Raven.Client.Documents.Session;

    class OpenRavenSessionByDatabaseName : IOpenTenantAwareRavenSessions
    {
        public OpenRavenSessionByDatabaseName(IDocumentStoreWrapper documentStoreWrapper, bool useClusterWideTransactions, Func<IDictionary<string, string>, string> getDatabaseName = null)
        {
            this.documentStoreWrapper = documentStoreWrapper;
            this.getDatabaseName = getDatabaseName ?? (context => string.Empty);
            this.useClusterWideTransactions = useClusterWideTransactions;
        }

        public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders)
        {
            var databaseName = getDatabaseName(messageHeaders);
            var documentSession = string.IsNullOrEmpty(databaseName)
                ? documentStoreWrapper.DocumentStore.OpenAsyncSession()
                : documentStoreWrapper.DocumentStore.OpenAsyncSession(databaseName);

            if (!useClusterWideTransactions)
            {
                documentSession.Advanced.UseOptimisticConcurrency = true;
            }

            return documentSession;
        }

        IDocumentStoreWrapper documentStoreWrapper;
        bool useClusterWideTransactions;
        Func<IDictionary<string, string>, string> getDatabaseName;
    }
}