namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Raven.Abstractions.Data;
    using Raven.Client.Document;

    static class DocumentStoreExtensionsForVoron
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
                // Task.Delay is being used to allow raven time to unlock the database 
                // Task.Run is being used to take the cleanup of the database out of the context of the test runner, as the test should not fail if cleanup fails.
                Task.Run(() =>
                {
                    Task.Delay(500);
                    store.DatabaseCommands.GlobalAdmin.DeleteDatabase(dbName, hardDelete: true);
                });
            }
        }
    }
}