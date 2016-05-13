namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    class DocumentStoreInitializer
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DocumentStoreInitializer));

        internal DocumentStoreInitializer(Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            this.storeCreator = storeCreator;
        }

        internal DocumentStoreInitializer(IDocumentStore store)
        {
            this.storeCreator = readOnlySettings => store;
        }

        public string Url => this.docStore?.Url;

        public string Identifier => this.docStore?.Identifier;

        internal IDocumentStore Init(ReadOnlySettings settings)
        {
            if (!isInitialized)
            {
                EnsureDocStoreCreated(settings);
                ApplyConventions(settings);
                BackwardsCompatibilityHelper.SupportOlderClrTypes(docStore);

                docStore.Initialize();
                StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(docStore, settings);
            }
            isInitialized = true;
            return docStore;
        }

        internal void EnsureDocStoreCreated(ReadOnlySettings settings)
        {
            if (docStore == null)
            {
                docStore = storeCreator(settings);
            }
        }

        void ApplyConventions(ReadOnlySettings settings)
        {
            if (DocumentIdConventionsExtensions.NeedToApplyDocumentIdConventionsToDocumentStore(settings))
            {
                var sagasEnabled = settings.IsFeatureActive(typeof(Sagas));
                var timeoutsEnabled = settings.IsFeatureActive(typeof(TimeoutManager));
                var idConventions = new DocumentIdConventions(docStore, settings.GetAvailableTypes(), settings.EndpointName().ToString(), sagasEnabled, timeoutsEnabled);
                docStore.Conventions.FindTypeTagName = idConventions.FindTypeTagName;
            }

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
            else 
            {
                if (store.JsonRequestFactory == null) // If the DocStore has not been initialized yet
                {
                    if (store.ResourceManagerId == Guid.Empty || store.ResourceManagerId == ravenDefaultResourceManagerId)
                    {
                        var resourceManagerId = settings.LocalAddress();
                        store.ResourceManagerId = DeterministicGuidBuilder(resourceManagerId);
                    }

                    // If using the default (Volatile - null should be impossible) then switch to IsolatedStorage
                    // Leave alone if LocalDirectoryTransactionRecoveryStorage!
                    if (store.TransactionRecoveryStorage == null || store.TransactionRecoveryStorage is VolatileOnlyTransactionRecoveryStorage)
                    {
                        store.TransactionRecoveryStorage = new IsolatedStorageTransactionRecoveryStorage();
                    }
                }

                var dtcSettingsNotIdeal = store.ResourceManagerId == Guid.Empty ||
                                          store.ResourceManagerId == ravenDefaultResourceManagerId ||
                                          !(store.TransactionRecoveryStorage is LocalDirectoryTransactionRecoveryStorage);

                if (dtcSettingsNotIdeal)
                {
                    Logger.Warn("NServiceBus has detected that a RavenDB DocumentStore is being used with Distributed Transaction Coordinator transactions, but without the recommended production-safe settings for ResourceManagerId or TransactionStorageRecovery. Please refer to \"Setting RavenDB DTC settings manually\" in the NServiceBus documentation for more information.");
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

        // Source: https://github.com/ravendb/ravendb/blob/f56963f23f54b5535eba4f043fb84d5145b11b1d/Raven.Client.Lightweight/Document/DocumentStore.cs#L129
        static readonly Guid ravenDefaultResourceManagerId = new Guid("e749baa6-6f76-4eef-a069-40a4378954f8");
        Func<ReadOnlySettings, IDocumentStore> storeCreator;
        IDocumentStore docStore;
        bool isInitialized;
    }
}
