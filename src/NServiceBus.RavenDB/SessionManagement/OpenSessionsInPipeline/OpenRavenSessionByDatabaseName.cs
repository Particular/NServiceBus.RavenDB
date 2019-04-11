namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Raven.Client.Documents.Session;

    class OpenRavenSessionByDatabaseName : IOpenRavenSessionsInPipeline
    {
        IDocumentStoreWrapper documentStoreWrapper;
        Func<IDictionary<string, string>, string> getDatabaseName;

        public OpenRavenSessionByDatabaseName(IDocumentStoreWrapper documentStoreWrapper, Func<IDictionary<string, string>, string> getDatabaseName = null)
        {
            this.documentStoreWrapper = documentStoreWrapper;
            this.getDatabaseName = getDatabaseName ?? (context => string.Empty);
        }

        public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders)
        {
            var databaseName = getDatabaseName(messageHeaders);
            var documentSession = string.IsNullOrEmpty(databaseName)
                ? documentStoreWrapper.DocumentStore.OpenAsyncSession()
                : documentStoreWrapper.DocumentStore.OpenAsyncSession(databaseName);

            documentSession.Advanced.AllowNonAuthoritativeInformation = false;
            documentSession.Advanced.UseOptimisticConcurrency = true;

            return documentSession;
        }
    }
}