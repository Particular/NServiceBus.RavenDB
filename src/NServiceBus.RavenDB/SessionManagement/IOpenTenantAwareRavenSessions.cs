namespace NServiceBus.Persistence.RavenDB
{
    using System.Collections.Generic;
    using Raven.Client.Documents.Session;

    interface IOpenTenantAwareRavenSessions
    {
        IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders, SessionOptions sessionOptions);
    }
}