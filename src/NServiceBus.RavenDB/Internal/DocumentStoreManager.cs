namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
#if NET452
    using System.Configuration;
    using System.Linq;
#endif
    using NServiceBus.Logging;
    using NServiceBus.Persistence;
    using NServiceBus.Settings;
    using Raven.Client;
    using Raven.Client.Document;

    static class DocumentStoreManager
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DocumentStoreManager));
        const string defaultDocStoreSettingsKey = "RavenDbDocumentStore";
        static Dictionary<Type, string> featureSettingsKeys;

#if NET452
        static Dictionary<Type, string> connStrKeys;
#endif

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

#if NET452
            connStrKeys = new Dictionary<Type, string>
            {
                {typeof(StorageType.GatewayDeduplication), "NServiceBus/Persistence/RavenDB/GatewayDeduplication"},
                {typeof(StorageType.Subscriptions), "NServiceBus/Persistence/RavenDB/Subscription"},
                {typeof(StorageType.Outbox), "NServiceBus/Persistence/RavenDB/Outbox"},
                {typeof(StorageType.Sagas), "NServiceBus/Persistence/RavenDB/Saga"},
                {typeof(StorageType.Timeouts), "NServiceBus/Persistence/RavenDB/Timeout"}
            };
#endif
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

#if NET452
            // Next, if a connection string name exists for the storage type, create based on that
            if (docStoreInitializer == null)
            {
                docStoreInitializer = CreateStoreByConnectionStringName(settings, connStrKeys[typeof(TStorageType)]);
            }
#endif

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
            var p = settings.GetOrDefault<ConnectionParameters>(RavenDbSettingsExtensions.DefaultConnectionParameters);
            if (p != null)
            {
                var storeByParams = new DocumentStore
                {
                    Url = p.Url,
                    DefaultDatabase = p.DatabaseName ?? settings.EndpointName(),
                    ApiKey = p.ApiKey,
                    Credentials = p.Credentials
                };

                return new DocumentStoreInitializer(storeByParams);
            }

#if NET452
            var initContext = CreateStoreByConnectionStringName(settings, "NServiceBus/Persistence/RavenDB", "NServiceBus/Persistence");

            if (initContext != null)
            {
                return initContext;
            }
#endif

            return CreateStoreByUrl(settings, "http://localhost:8080");
        }

        static DocumentStoreInitializer CreateStoreByUrl(ReadOnlySettings settings, string url)
        {
            var docStore = new DocumentStore
            {
                Url = url
            };

            if (docStore.DefaultDatabase == null)
            {
                docStore.DefaultDatabase = settings.EndpointName();
            }

            return new DocumentStoreInitializer(docStore);
        }

#if NET452

        static DocumentStoreInitializer CreateStoreByConnectionStringName(ReadOnlySettings settings, params string[] connectionStringNames)
        {
            var connectionStringName = GetFirstNonEmptyConnectionString(connectionStringNames);
            if (!string.IsNullOrWhiteSpace(connectionStringName))
            {
                var docStore = new DocumentStore
                {
                    ConnectionStringName = connectionStringName
                };
                if (docStore.DefaultDatabase == null)
                {
                    docStore.DefaultDatabase = settings.EndpointName();
                }

                return new DocumentStoreInitializer(docStore);
            }
            return null;
        }

        static string GetFirstNonEmptyConnectionString(params string[] connectionStringNames)
        {
            try
            {
                var foundConnectionStringNames = connectionStringNames.Where(name => ConfigurationManager.ConnectionStrings[name] != null).ToArray();
                var firstFound = foundConnectionStringNames.FirstOrDefault();

                if (foundConnectionStringNames.Length > 1)
                {
                    Logger.Warn($"Multiple possible RavenDB connection strings found. Using connection string `{firstFound}`.");
                }

                return firstFound;
            }
            catch (ConfigurationErrorsException)
            {
                return null;
            }
        }

#endif
    }
}

