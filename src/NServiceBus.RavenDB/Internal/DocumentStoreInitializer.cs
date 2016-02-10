namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
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
            docStore.Conventions.FindTypeTagName = BackwardsCompatibilityHelper.LegacyFindTypeTagName;

            var store = docStore as DocumentStore;
            if (store == null)
            {
                return;
            }

            // Source: https://github.com/ravendb/ravendb/blob/f56963f23f54b5535eba4f043fb84d5145b11b1d/Raven.Client.Lightweight/Document/DocumentStore.cs#L129
            var ravenDefaultResourceManagerId = new Guid("e749baa6-6f76-4eef-a069-40a4378954f8");

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

            store.TransactionRecoveryStorage = CreateValidTransactionRecoveryStorage(settings, store.ResourceManagerId);

            bool suppressDistributedTransactions;
            if (settings.TryGet("Transactions.SuppressDistributedTransactions", out suppressDistributedTransactions) && suppressDistributedTransactions)
            {
                store.EnlistInDistributedTransactions = false;
            }
        }

        static ITransactionRecoveryStorage CreateValidTransactionRecoveryStorage(ReadOnlySettings settings, Guid resourceManagerId)
        {
            var suffixPath = Path.Combine("NServiceBus.RavenDB", resourceManagerId.ToString());

            foreach (var basePath in GetPotentialTransactionRecoveryStoragePaths(settings))
            {
                var fullPath = Path.Combine(basePath, suffixPath);
                try
                {
                    return new LocalDirectoryTransactionRecoveryStorage(fullPath);
                }
                // TODO: Cover other exception types? https://msdn.microsoft.com/en-us/library/54a0at6s(v=vs.110).aspx
                catch (UnauthorizedAccessException)
                {
                    // Have to catch and move on to the next location
                }
            }

            Logger.Warn($"Unable to find a location to store RavenDB transaction recovery storage information. Consider specifying a writeable directory using the `{RavenDbPersistenceSettingsExtensions.TransactionRecoveryStorageBasePathKey}` appSetting or .SetTransactionRecoveryStorageBasePath(basePath) option.");
            Logger.Warn("Falling back to IsolatedStorage-based transaction recovery storage, which has shown to be unstable in high-contention situations.");

            return new IsolatedStorageTransactionRecoveryStorage();
        }

        static IEnumerable<string> GetPotentialTransactionRecoveryStoragePaths(ReadOnlySettings settings)
        {
            string settingsBasePath;
            if (settings.TryGet(RavenDbPersistenceSettingsExtensions.TransactionRecoveryStorageBasePathKey, out settingsBasePath))
            {
                yield return settingsBasePath;
                throw new Exception($"Unable to access RavenDB transaction recovery storage specified by `.SetTransactionRecoveryStorageBasePath(string basePath)` because access to `{settingsBasePath}` is denied.");
            }

            var configBasePath = ConfigurationManager.AppSettings[RavenDbPersistenceSettingsExtensions.TransactionRecoveryStorageBasePathKey];
            if (configBasePath != null)
            {
                yield return configBasePath;
                throw new ConfigurationErrorsException($"Unable to access RavenDB transaction recovery storage specified in `RavenDB/TransactionRecoveryStorage/BasePath` appSetting because access to `{configBasePath}`is denied.");
            }

            yield return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        }

        static Guid DeterministicGuidBuilder(string input)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new SHA1CryptoServiceProvider())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = provider.ComputeHash(inputBytes);
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
