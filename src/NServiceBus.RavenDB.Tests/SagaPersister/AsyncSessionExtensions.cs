using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using Raven.Client.Documents.Session;

static class AsyncSessionExtensions
{
    public static async ValueTask<RavenDBSynchronizedStorageSession> CreateSynchronizedSession(this IAsyncDocumentSession session, ContextBag options, CancellationToken cancellationToken = default)
    {
        var outboxTransaction = new RavenDBOutboxTransaction(session);
        var synchronizedStorageSession = new RavenDBSynchronizedStorageSession(null);
        _ = await synchronizedStorageSession.TryOpen(outboxTransaction, options, cancellationToken);
        return synchronizedStorageSession;
    }
}