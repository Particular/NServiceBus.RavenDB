using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using Raven.Client.Documents.Session;

static class AsyncSessionExtensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Code", "PS0018:A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext", Justification = "<Pending>")]
    public static async Task<RavenDBSynchronizedStorageSession> CreateSynchronizedSession(this IAsyncDocumentSession session, ContextBag options)
    {
        var outboxTransaction = new RavenDBOutboxTransaction(session);
        var synchronizedStorageSession = new RavenDBSynchronizedStorageSession(null);
        await synchronizedStorageSession.TryOpen(outboxTransaction, options, CancellationToken.None);
        return synchronizedStorageSession;
    }
}