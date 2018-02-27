namespace NServiceBus.Persistence.RavenDB
{
    using System.Collections.Generic;
    using Raven.Client;

    interface IOpenRavenSessionsInPipeline
    {
        IDocumentSession OpenSession(IDictionary<string, string> headers);
    }
}