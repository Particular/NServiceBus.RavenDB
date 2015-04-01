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
            var dataDir = $"~/VoronTest-{Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()}";
            store.DatabaseCommands.GlobalAdmin.CreateDatabase(new DatabaseDocument
            {
                Id = "VoronTest",
                Settings = new Dictionary<string, string>
                    {
                        { "Raven/StorageTypeName", "voron" },
                        { "Raven/DataDir", dataDir }
                    }
            });
            store.DefaultDatabase = "VoronTest";
            return new VoronTestDeleter(store);
        }

        class VoronTestDeleter : IDisposable
        {
            readonly DocumentStore store;

            public VoronTestDeleter(DocumentStore store)
            {
                this.store = store;
            }

            public void Dispose()
            {
                store.DatabaseCommands.GlobalAdmin.DeleteDatabase("VoronTest", hardDelete: true);
            }
        }
    }
}