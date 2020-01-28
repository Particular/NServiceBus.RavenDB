namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using Raven.Client.Documents;

    static class DocumentStoreManager
    {
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
            var initContext = new DocumentStoreInitializer((s, _) => storeCreator(s));
            settings.Set(featureSettingsKeys[storageType], initContext);
        }

        public static void SetDefaultStore(SettingsHolder settings, IDocumentStore documentStore)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }
            SetDefaultStore(settings, (ReadOnlySettings s) => documentStore);
        }

        public static void SetDefaultStore(SettingsHolder settings, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            if (storeCreator == null)
            {
                throw new ArgumentNullException(nameof(storeCreator));
            }
            var initContext = new DocumentStoreInitializer((s, _) => storeCreator(s));
            settings.Set(defaultDocStoreSettingsKey, initContext);
        }

        public static void SetDefaultStore(SettingsHolder settings, Func<IBuilder, IDocumentStore> storeCreator)
        {
            if (storeCreator == null)
            {
                throw new ArgumentNullException(nameof(storeCreator));
            }
            var initContext = new DocumentStoreInitializer((_, builder) => storeCreator(builder));
            settings.Set(defaultDocStoreSettingsKey, initContext);
        }

        public static IDocumentStore GetDocumentStore<TStorageType>(ReadOnlySettings settings, IBuilder builder)
            where TStorageType : StorageType
        {
            return GetUninitializedDocumentStore<TStorageType>(settings).Init(settings, builder);
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

            if (docStoreInitializer == null)
            {
                throw new Exception($"In order to use RavenDB as persistence for {typeof(TStorageType).Name}, a DocumentStore instance or builder must be set using persistence.{nameof(RavenDbSettingsExtensions.SetDefaultDocumentStore)}(...).");
            }

            return docStoreInitializer;
        }
    }
}
