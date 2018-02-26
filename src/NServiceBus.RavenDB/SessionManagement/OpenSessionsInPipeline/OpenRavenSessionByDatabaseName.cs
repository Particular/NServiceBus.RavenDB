namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.RavenDB.Internal;
    using Raven.Client;

    class OpenRavenSessionByDatabaseName : IOpenRavenSessionsInPipeline
    {
        IDocumentStoreWrapper documentStoreWrapper;
        Func<IMessageContext, string> getDatabaseName;

        public OpenRavenSessionByDatabaseName(IDocumentStoreWrapper documentStoreWrapper, Func<IMessageContext, string> getDatabaseName = null)
        {
            this.documentStoreWrapper = documentStoreWrapper;
            this.getDatabaseName = getDatabaseName ?? (context => string.Empty);
        }

        public IDocumentSession OpenSession(IDictionary<string, string> headers)
        {
            var messageContext = new FakeMessageContextContainingOnlyHeaders(headers);

            var databaseName = getDatabaseName(messageContext);
            var documentSession = string.IsNullOrEmpty(databaseName)
                ? documentStoreWrapper.DocumentStore.OpenSession()
                : documentStoreWrapper.DocumentStore.OpenSession(databaseName);

            documentSession.Advanced.AllowNonAuthoritativeInformation = false;
            documentSession.Advanced.UseOptimisticConcurrency = true;

            return documentSession;
        }
    }
}