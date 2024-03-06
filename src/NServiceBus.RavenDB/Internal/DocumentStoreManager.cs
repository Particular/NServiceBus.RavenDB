namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Settings;
    using Raven.Client.Documents;

    static class DocumentStoreManager
    {
        static DocumentStoreManager()
        {
            featureSettingsKeys = new Dictionary<Type, string>
            {
                {typeof(StorageType.Subscriptions), "RavenDbDocumentStore/Subscription"},
                {typeof(StorageType.Outbox), "RavenDbDocumentStore/Outbox"},
                {typeof(StorageType.Sagas), "RavenDbDocumentStore/Saga"},
            };
        }

        public static void SetDocumentStore<TStorageType>(SettingsHolder settings, IDocumentStore documentStore)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }

            SetDocumentStoreInternal(settings, typeof(TStorageType), (_, __) => documentStore);
        }

        public static void SetDocumentStore<TStorageType>(SettingsHolder settings, Func<IReadOnlySettings, IDocumentStore> storeCreator)
            where TStorageType : StorageType
        {
            if (storeCreator == null)
            {
                throw new ArgumentNullException(nameof(storeCreator));
            }

            SetDocumentStoreInternal(settings, typeof(TStorageType), (s, _) => storeCreator(s));
        }

        public static void SetDocumentStore<TStorageType>(SettingsHolder settings, Func<IReadOnlySettings, IServiceProvider, IDocumentStore> storeCreator)
            where TStorageType : StorageType
        {
            if (storeCreator == null)
            {
                throw new ArgumentNullException(nameof(storeCreator));
            }

            SetDocumentStoreInternal(settings, typeof(TStorageType), storeCreator);
        }

        static void SetDocumentStoreInternal(SettingsHolder settings, Type storageType, Func<IReadOnlySettings, IServiceProvider, IDocumentStore> storeCreator)
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

            SetDefaultStoreInternal(settings, (_, __) => documentStore);
        }

        public static void SetDefaultStore(SettingsHolder settings, Func<IReadOnlySettings, IDocumentStore> storeCreator)
        {
            if (storeCreator == null)
            {
                throw new ArgumentNullException(nameof(storeCreator));
            }

            SetDefaultStoreInternal(settings, (s, _) => storeCreator(s));
        }

        public static void SetDefaultStore(SettingsHolder settings, Func<IReadOnlySettings, IServiceProvider, IDocumentStore> storeCreator)
        {
            if (storeCreator == null)
            {
                throw new ArgumentNullException(nameof(storeCreator));
            }

            SetDefaultStoreInternal(settings, storeCreator);
        }

        static void SetDefaultStoreInternal(SettingsHolder settings, Func<IReadOnlySettings, IServiceProvider, IDocumentStore> storeCreator)
        {
            var initContext = new DocumentStoreInitializer(storeCreator);
            settings.Set(defaultDocStoreSettingsKey, initContext);
        }

        public static IDocumentStore GetDocumentStore<TStorageType>(IReadOnlySettings settings, IServiceProvider builder)
            where TStorageType : StorageType
        {
            return GetUninitializedDocumentStore<TStorageType>(settings).Init(settings, builder);
        }

        internal static DocumentStoreInitializer GetUninitializedDocumentStore<TStorageType>(IReadOnlySettings settings)
            where TStorageType : StorageType
        {
            // First try to get a document store specific to a storage type (Subscriptions, Gateway, etc.)
            var docStoreInitializer = settings.GetOrDefault<DocumentStoreInitializer>(featureSettingsKeys[typeof(TStorageType)]);

            // Next try finding a shared DocumentStore
            docStoreInitializer ??= settings.GetOrDefault<DocumentStoreInitializer>(defaultDocStoreSettingsKey);

            if (docStoreInitializer == null)
            {
                throw new Exception($"In order to use RavenDB as persistence for {typeof(TStorageType).Name}, a DocumentStore instance or builder must be set using persistence.{nameof(RavenDbSettingsExtensions.SetDefaultDocumentStore)}(...).");
            }

            return docStoreInitializer;
        }

        const string defaultDocStoreSettingsKey = "RavenDbDocumentStore";
        static Dictionary<Type, string> featureSettingsKeys;
    }
}