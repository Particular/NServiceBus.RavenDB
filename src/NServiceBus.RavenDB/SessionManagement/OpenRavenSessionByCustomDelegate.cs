namespace NServiceBus.Persistence.RavenDB;

using System;
using System.Collections.Generic;
using Raven.Client.Documents.Session;

class OpenRavenSessionByCustomDelegate : IOpenTenantAwareRavenSessions
{
    public OpenRavenSessionByCustomDelegate(Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSession, bool useClusterWideTransactions)
    {
        getAsyncSessionUsingHeaders = getAsyncSession;
        this.useClusterWideTransactions = useClusterWideTransactions;
    }

    public IAsyncDocumentSession OpenSession(IDictionary<string, string> messageHeaders)
    {
        var session = getAsyncSessionUsingHeaders(messageHeaders);

        if (!useClusterWideTransactions)
        {
            session.Advanced.UseOptimisticConcurrency = true;
        }

        return session;
    }

    Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSessionUsingHeaders;
    readonly bool useClusterWideTransactions;
}