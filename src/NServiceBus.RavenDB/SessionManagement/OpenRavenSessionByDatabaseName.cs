namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Raven.Client.Documents.Session;
    using Settings;

    class OpenRavenSessionByDatabaseName : IOpenTenantAwareRavenSessions
    {
        public OpenRavenSessionByDatabaseName(IDocumentStoreWrapper documentStoreWrapper, ReadOnlySettings settings, Func<IDictionary<string, string>, string> getDatabaseName = null)
        {
            this.documentStoreWrapper = documentStoreWrapper;
            this.settings = settings;
            this.getDatabaseName = getDatabaseName ?? (context => string.Empty);
        }

        public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders)
        {
            var databaseName = getDatabaseName(messageHeaders);
            IAsyncDocumentSession documentSession;
            var useClusterWideTx = settings.GetOrDefault<bool>(RavenDbStorageSession.UseClusterWideTransactions);

            var options = new SessionOptions();
            if (useClusterWideTx)
            {
                options.TransactionMode = TransactionMode.ClusterWide;
            }
            if (string.IsNullOrEmpty(databaseName))
            {
                documentSession = documentStoreWrapper.DocumentStore.OpenAsyncSession(options);
            }
            else
            {
                options.Database = databaseName;
                documentSession = documentStoreWrapper.DocumentStore.OpenAsyncSession(options);
            }

            documentSession.Advanced.UseOptimisticConcurrency = !useClusterWideTx;

            return documentSession;
        }

        IDocumentStoreWrapper documentStoreWrapper;
        readonly ReadOnlySettings settings;
        Func<IDictionary<string, string>, string> getDatabaseName;
    }
}