namespace NServiceBus.Persistence.RavenDB
{
    using System.Collections.Generic;
    using Raven.Client;

    interface IOpenRavenSessionsInPipeline
    {
        IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders);
    }
}