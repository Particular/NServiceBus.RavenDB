namespace NServiceBus.Persistence.RavenDB
{
    using System.Collections.Generic;
    using Raven.Client.Documents.Session;

    //TODO rename this e.g. IOpenTenantAwareRavenSessions
    interface IOpenRavenSessionsInPipeline
    {
        IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders);
    }
}