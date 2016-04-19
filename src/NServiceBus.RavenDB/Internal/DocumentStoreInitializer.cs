namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    class DocumentStoreInitializer
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DocumentStoreInitializer));

        internal DocumentStoreInitializer(IDocumentStore store)
        {
            this.docStore = store;
        }

        public string Url => this.docStore.Url;

        public string Identifier => this.docStore.Identifier;

        internal IDocumentStore Init(ReadOnlySettings settings)
        {
            if (!isInitialized)
            {
                ApplyConventions(settings);
                BackwardsCompatibilityHelper.SupportOlderClrTypes(docStore);

                docStore.Initialize();
                StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(docStore, settings);
            }
            isInitialized = true;
            return docStore;
        }

        void ApplyConventions(ReadOnlySettings settings)
        {
            var idConventions = new DocumentIdConventions(docStore, settings.GetAvailableTypes(), settings.EndpointName().ToString());
            docStore.Conventions.FindTypeTagName = idConventions.FindTypeTagName;

            var store = docStore as DocumentStore;
            if (store == null)
            {
                return;
            }

            bool suppressDistributedTransactions;
            if (settings.TryGet("Transactions.SuppressDistributedTransactions", out suppressDistributedTransactions) && suppressDistributedTransactions)
            {
                store.EnlistInDistributedTransactions = false;
            }
            else if (store.JsonRequestFactory == null) // If the DocStore has not been initialized yet
            {
                // Source: https://github.com/ravendb/ravendb/blob/f56963f23f54b5535eba4f043fb84d5145b11b1d/Raven.Client.Lightweight/Document/DocumentStore.cs#L129
                var ravenDefaultResourceManagerId = new Guid("e749baa6-6f76-4eef-a069-40a4378954f8");

                if (store.ResourceManagerId == Guid.Empty || store.ResourceManagerId == ravenDefaultResourceManagerId)
                {
                    // TODO: Temporary Fix - In 3.x calculation used EndpointVersion as well
                    // See https://github.com/Particular/NServiceBus.RavenDB/issues/178
                    var resourceManagerId = settings.LocalAddress(); // + "-" + settings.Get<string>("EndpointVersion");
                    store.ResourceManagerId = DeterministicGuidBuilder(resourceManagerId);
                }

                // If using the default (Volatile - null should be impossible) then switch to IsolatedStorage
                // Leave alone if LocalDirectoryTransactionRecoveryStorage!
                if (store.TransactionRecoveryStorage == null || store.TransactionRecoveryStorage is VolatileOnlyTransactionRecoveryStorage)
                {
                    store.TransactionRecoveryStorage = new IsolatedStorageTransactionRecoveryStorage();
                }
            }
        }

        static Guid DeterministicGuidBuilder(string input)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(input);
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }

        IDocumentStore docStore;
        bool isInitialized;
    }
}
