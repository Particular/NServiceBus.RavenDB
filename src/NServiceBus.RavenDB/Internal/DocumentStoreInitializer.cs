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
                var customize = DocumentStoreManager.GetCustomizeDocumentStoreDelegate(settings);

                ApplyConventions(settings);
                BackwardsCompatibilityHelper.SupportOlderClrTypes(docStore);

                if (customize != null)
                {
                    customize(docStore);
                }

                docStore.Initialize();
                StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(docStore, settings);
            }
            isInitialized = true;
            return docStore;
        }

        void ApplyConventions(ReadOnlySettings settings)
        {
            var idConventions = new DocumentIdConventions(docStore, settings.GetAvailableTypes());
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
            else
            {
                // Source: https://github.com/ravendb/ravendb/blob/f56963f23f54b5535eba4f043fb84d5145b11b1d/Raven.Client.Lightweight/Document/DocumentStore.cs#L129
                var ravenDefaultResourceManagerId = new Guid("e749baa6-6f76-4eef-a069-40a4378954f8");


                if (store.JsonRequestFactory != null) // code for "has the DocStore been Initialized yet"
                {
                    // If the DocumentStore has already been initialized, that means transaction recovery has already
                    // been run and the TransactionRecoveryStorage cannot be safely swapped with our safe values.
                    // Only (known) reason to do this would be if customer wants to share the DocumentStore with
                    // a wider application's persistence needs and must initialize it before NServiceBus is invoked.
                    // Therefore, we will check to make sure ResourceManagerId and TransactionRecoveryStorage are
                    // acceptable and throw if they are not.

                    var dtcSettingsAcceptable = store.ResourceManagerId != Guid.Empty
                                                && store.ResourceManagerId != ravenDefaultResourceManagerId
                                                && store.TransactionRecoveryStorage is LocalDirectoryTransactionRecoveryStorage;

                    if (!dtcSettingsAcceptable)
                    {
                        throw new InvalidOperationException("RavenDB Persistence DocumentStore has already been initialized without safe DTC settings which can lead to data loss. Either allow NServiceBus to initialize the DocumentStore instance or refer to documentation to set up safe DTC settings.");
                    }
                }
                else
                {
                    if (store.ResourceManagerId != ravenDefaultResourceManagerId)
                    {
                        Logger.Warn("Overriding user-specified documentStore.ResourceManagerId. It's no longer necessary to set this value while using NServiceBus.RavenDB persistence. Consider using busConfiguration.CustomizeDocumentStore(Action<IDocumentStore customize) if this is absolutely necessary, but it is not recommended.");
                    }
                    if (!(store.TransactionRecoveryStorage is VolatileOnlyTransactionRecoveryStorage))
                    {
                        Logger.Warn("Overriding user-specified documentStore.TransactionRecoveryStorage. It's no longer necessary to set this value while using NServiceBus.RavenDB persistence. Consider using busConfiguration.CustomizeDocumentStore(Action<IDocumentStore customize) if this is absolutely necessary, but it is not recommended.");
                    }

                    var resourceManagerId = settings.Get<string>("NServiceBus.LocalAddress") + "-" + settings.Get<string>("EndpointVersion");
                    store.ResourceManagerId = DeterministicGuidBuilder(resourceManagerId);

                    store.TransactionRecoveryStorage = RavenTransactionRecoveryStorageCreator.Create(docStore, settings);
                }
            }
        }

        static Guid DeterministicGuidBuilder(string input)
        {
            // use SHA1 hash to get a hash of the string - SHA1 to avoid FIPS troubles
            using (var provider = new SHA1CryptoServiceProvider())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = provider.ComputeHash(inputBytes);

                // Guids only need 16 bytes. SHA1 hashes are 20 bytes
                var first16HashBytes = new byte[16];
                Array.Copy(hashBytes, first16HashBytes, 16);

                // generate a guid from the hash:
                return new Guid(first16HashBytes);
            }
        }

        IDocumentStore docStore;
        bool isInitialized;
    }
}
