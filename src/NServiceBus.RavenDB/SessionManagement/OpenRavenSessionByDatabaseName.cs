namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Raven.Client.Documents.Session;

    class OpenRavenSessionByDatabaseName : IOpenTenantAwareRavenSessions
    {
        public OpenRavenSessionByDatabaseName(IDocumentStoreWrapper documentStoreWrapper, SagaPersistenceConfiguration sagaPersistenceConfiguration, Func<IDictionary<string, string>, string> getDatabaseName = null)
        {
            this.documentStoreWrapper = documentStoreWrapper;
            this.sagaPersistenceConfiguration = sagaPersistenceConfiguration;
            this.getDatabaseName = getDatabaseName ?? (context => string.Empty);
        }

        public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders)
        {
            var databaseName = getDatabaseName(messageHeaders);
            var documentSession = string.IsNullOrEmpty(databaseName)
                ? documentStoreWrapper.DocumentStore.OpenAsyncSession()
                : documentStoreWrapper.DocumentStore.OpenAsyncSession(databaseName);

            documentSession.Advanced.UseOptimisticConcurrency = !sagaPersistenceConfiguration.EnablePessimisticLocking;

            return documentSession;
        }

        IDocumentStoreWrapper documentStoreWrapper;
        SagaPersistenceConfiguration sagaPersistenceConfiguration;
        Func<IDictionary<string, string>, string> getDatabaseName;
    }
}