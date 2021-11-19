namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Raven.Client.Documents.Session;

    class OpenRavenSessionByDatabaseName : IOpenTenantAwareRavenSessions
    {
        public OpenRavenSessionByDatabaseName(IDocumentStoreWrapper documentStoreWrapper, Func<IDictionary<string, string>, string> getDatabaseName = null)
        {
            this.documentStoreWrapper = documentStoreWrapper;
            this.getDatabaseName = getDatabaseName ?? (context => string.Empty);
        }

        public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders, SessionOptions sessionOptions)
        {
            var databaseName = getDatabaseName(messageHeaders);
            if (!string.IsNullOrEmpty(databaseName))
            {
                sessionOptions.Database = databaseName;
            }

            var documentSession = documentStoreWrapper.DocumentStore.OpenAsyncSession(sessionOptions);
            documentSession.Advanced.UseOptimisticConcurrency = true;

            return documentSession;
        }

        IDocumentStoreWrapper documentStoreWrapper;
        Func<IDictionary<string, string>, string> getDatabaseName;
    }
}