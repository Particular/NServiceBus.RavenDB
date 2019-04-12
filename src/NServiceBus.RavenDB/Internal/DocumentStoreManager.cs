namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using Raven.Client.Documents;

    static class DocumentStoreManager
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DocumentStoreManager));
        const string defaultDocStoreSettingsKey = "RavenDbDocumentStore";
        static Dictionary<Type, string> featureSettingsKeys;

        static DocumentStoreManager()
        {
            featureSettingsKeys = new Dictionary<Type, string>
            {
                {typeof(StorageType.GatewayDeduplication), "RavenDbDocumentStore/GatewayDeduplication"},
                {typeof(StorageType.Subscriptions), "RavenDbDocumentStore/Subscription"},
                {typeof(StorageType.Outbox), "RavenDbDocumentStore/Outbox"},
                {typeof(StorageType.Sagas), "RavenDbDocumentStore/Saga"},
                {typeof(StorageType.Timeouts), "RavenDbDocumentStore/Timeouts"}
            };
        }

        public static void SetDocumentStore<TStorageType>(SettingsHolder settings, IDocumentStore documentStore)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }
            SetDocumentStoreInternal(settings, typeof(TStorageType), s => documentStore);
        }

        public static void SetDocumentStore<TStorageType>(SettingsHolder settings, Func<ReadOnlySettings, IDocumentStore> storeCreator)
            where TStorageType : StorageType
        {
            if (storeCreator == null)
            {
                throw new ArgumentNullException(nameof(storeCreator));
            }
            SetDocumentStoreInternal(settings, typeof(TStorageType), storeCreator);
        }

        private static void SetDocumentStoreInternal(SettingsHolder settings, Type storageType, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            var initContext = new DocumentStoreInitializer(storeCreator);
            settings.Set(featureSettingsKeys[storageType], initContext);
        }

        public static void SetDefaultStore(SettingsHolder settings, IDocumentStore documentStore)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }
            SetDefaultStore(settings, s => documentStore);
        }

        public static void SetDefaultStore(SettingsHolder settings, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            if (storeCreator == null)
            {
                throw new ArgumentNullException(nameof(storeCreator));
            }
            var initContext = new DocumentStoreInitializer(storeCreator);
            settings.Set(defaultDocStoreSettingsKey, initContext);
        }

        public static IDocumentStore GetDocumentStore<TStorageType>(ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return GetUninitializedDocumentStore<TStorageType>(settings).Init(settings);
        }

        internal static DocumentStoreInitializer GetUninitializedDocumentStore<TStorageType>(ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            // First try to get a document store specific to a storage type (Subscriptions, Gateway, etc.)
            var docStoreInitializer = settings.GetOrDefault<DocumentStoreInitializer>(featureSettingsKeys[typeof(TStorageType)]);

            // Next try finding a shared DocumentStore
            if (docStoreInitializer == null)
            {
                docStoreInitializer = settings.GetOrDefault<DocumentStoreInitializer>(defaultDocStoreSettingsKey);
            }

            // Otherwise, we need to create it ourselves, but do so only once.
            if (docStoreInitializer == null)
            {
                // The holder is known to be non-null since we set it in SharedDocumentStore feature ctor
                var holder = settings.Get<SingleSharedDocumentStore>();
                if (holder.Initializer == null)
                {
                    holder.Initializer = CreateDefaultDocumentStore(settings);
                }

                docStoreInitializer = holder.Initializer;
            }

            if (docStoreInitializer == null)
            {
                throw new Exception($"RavenDB is configured as persistence for {typeof(TStorageType).Name} and no DocumentStore instance could be found.");
            }

            return docStoreInitializer;
        }

        private static DocumentStoreInitializer CreateDefaultDocumentStore(ReadOnlySettings settings)
        {
            return CreateStoreByUrl(settings, "http://localhost:8080");
        }

        static DocumentStoreInitializer CreateStoreByUrl(ReadOnlySettings settings, string url)
        {
            var docStore = new DocumentStore
            {
                Urls = new[] { url }
            };

            if (docStore.Database == null)
            {
                docStore.Database = settings.EndpointName();
            }

            return new DocumentStoreInitializer(docStore);
        }
    }
}
