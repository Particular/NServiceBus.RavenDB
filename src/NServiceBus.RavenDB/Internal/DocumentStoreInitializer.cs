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
            var sagasEnabled = settings.GetOrDefault<bool>(typeof(Features.Sagas).FullName);
            var timeoutsEnabled = settings.GetOrDefault<bool>(typeof(Features.TimeoutManager).FullName);
            var idConventions = new DocumentIdConventions(docStore, settings.GetAvailableTypes(), settings.EndpointName(), sagasEnabled, timeoutsEnabled);
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
                Logger.Error("It's possible for RavenDB to lose data when used with Distributed Transaction Coordinator (DTC) transactions. Future versions of NServiceBus RavenDB Persistence will not support this combination. If using the same RavenDB database for NServiceBus data and all business data, you can disable enlisting by calling Transactions().DisableDistributedTransactions() on the BusConfiguration instance and enable the Outbox feature to maintain consistency between messaging operations and data persistence. See 'DTC not supported for RavenDB Persistence' in the documentation for more details.");

                if (store.JsonRequestFactory == null) // If the DocStore has not been initialized yet
                {
                    // Source: https://github.com/ravendb/ravendb/blob/f56963f23f54b5535eba4f043fb84d5145b11b1d/Raven.Client.Lightweight/Document/DocumentStore.cs#L129
                    var ravenDefaultResourceManagerId = new Guid("e749baa6-6f76-4eef-a069-40a4378954f8");

                    if (store.ResourceManagerId == Guid.Empty || store.ResourceManagerId == ravenDefaultResourceManagerId)
                    {
                        var resourceManagerId = settings.Get<string>("NServiceBus.LocalAddress") + "-" + settings.Get<string>("EndpointVersion");
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
