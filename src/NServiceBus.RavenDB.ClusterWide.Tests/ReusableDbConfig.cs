namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Raven.Client.Documents;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;

    partial class ReusableDB
    {
        public async Task EnsureDatabaseExists(IDocumentStore store, CancellationToken cancellationToken = default)
        {
            // The Raven client does this as a courtesy but may fail. During tests a race condition could
            // prevent it from existing in time. So we are forcing the issue. In real life, every connection
            // to the server ever attempting to ensure its existence means we can rely on it.
            var dbRecord = new DatabaseRecord(databaseName)
            {
                Topology = new DatabaseTopology() { Members = new List<string> { "A", "B", "C" } }
            };
            await store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(dbRecord), cancellationToken);
            Console.WriteLine($"Provisioned new Raven database name {databaseName}");
        }

        public bool GetTransactionMode => true;
    }
}