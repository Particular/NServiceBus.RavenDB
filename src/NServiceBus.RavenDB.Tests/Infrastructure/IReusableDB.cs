namespace NServiceBus.RavenDB.Tests;

using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Documents;

interface IReusableDB
{
    Task EnsureDatabaseExists(IDocumentStore store, CancellationToken cancellationToken = default);
    bool UseClusterWideTransactions { get; }
    IDocumentStore NewStore(string identifier = null);
    IDocumentStore CreateStore();
    Task WaitForIndexing(IDocumentStore store, CancellationToken cancellationToken = default);
    void Dispose();
}