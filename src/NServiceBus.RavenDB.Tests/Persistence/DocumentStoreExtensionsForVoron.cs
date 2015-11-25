namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using System.Collections.Generic;
    using Raven.Abstractions.Data;
    using Raven.Client.Document;

    internal static class DocumentStoreExtensionsForVoron
    {
        public static IDisposable SetupVoronTest(this DocumentStore store)
        {
            store.Initialize();
            var dbName = $"VoronTest-{Guid.NewGuid().ToString("N").ToUpper()}";
            var dataDir = "~/" + dbName;
            store.DatabaseCommands.GlobalAdmin.CreateDatabase(new DatabaseDocument
            {
                Id = dbName,
                Settings = new Dictionary<string, string>
                    {
                        { "Raven/StorageTypeName", "voron" },
                        { "Raven/DataDir", dataDir }
                    }
            });
            store.DefaultDatabase = dbName;
            store.DatabaseCommands.GlobalAdmin.EnsureDatabaseExists(dbName);
            return new VoronTestDeleter(store, dbName);
        }

        class VoronTestDeleter : IDisposable
        {
            readonly DocumentStore store;
            readonly string dbName;

            public VoronTestDeleter(DocumentStore store, string dbName)
            {
                this.store = store;
                this.dbName = dbName;
            }

            public void Dispose()
            {
                store.DatabaseCommands.GlobalAdmin.DeleteDatabase(dbName, hardDelete: true);
            }
        }
    }
}