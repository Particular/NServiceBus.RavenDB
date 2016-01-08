namespace NServiceBus.RavenDB.Internal
{
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    static class TransactionRecoveryStorageVerifier
    {
        public static void ReplaceStorageIfNotSetByUser(IDocumentStore store)
        {
            var docStore = store as DocumentStore;

            if (docStore == null)
                return;

            var currentStorage = docStore.TransactionRecoveryStorage;

            // VolatileOnlyTransactionRecoveryStorage is the default. If set to anything else, don't change it!
            if (currentStorage == null || currentStorage is VolatileOnlyTransactionRecoveryStorage)
            {
                docStore.TransactionRecoveryStorage = new IsolatedStorageTransactionRecoveryStorage();
            }
        }
    }
}
