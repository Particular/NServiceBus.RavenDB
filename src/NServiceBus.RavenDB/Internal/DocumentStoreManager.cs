namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using NServiceBus.Logging;
    using NServiceBus.Persistence;
    using NServiceBus.Settings;
    using Raven.Client;
    using Raven.Client.Document;

    static class DocumentStoreManager
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DocumentStoreManager));
        const string settingsKey = "RavenDbDocumentStore";
        const string CustomizeDocumentStoreKey = "RavenDB.SetCustomizeDocumentStoreDelegate";
        static Dictionary<Type, string> featureSettingsKeys;
        static Dictionary<Type, string> connStrKeys;

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

            connStrKeys = new Dictionary<Type, string>()
            {
                {typeof(StorageType.GatewayDeduplication), "NServiceBus/Persistence/RavenDB/GatewayDeduplication"},
                {typeof(StorageType.Subscriptions), "NServiceBus/Persistence/RavenDB/Subscription"},
                {typeof(StorageType.Outbox), "NServiceBus/Persistence/RavenDB/Outbox"},
                {typeof(StorageType.Sagas), "NServiceBus/Persistence/RavenDB/Saga"},
                {typeof(StorageType.Timeouts), "NServiceBus/Persistence/RavenDB/Timeout"}
            };
        }

        public static void SetDocumentStore<TStorageType>(SettingsHolder settings, IDocumentStore store)
            where TStorageType : StorageType
        {
            var initContext = new DocumentStoreInitContext(store);
            settings.Set(featureSettingsKeys[typeof(TStorageType)], initContext);
        }

        public static void SetDefaultStore(SettingsHolder settings, IDocumentStore store)
        {
            var initContext = new DocumentStoreInitContext(store);
            settings.Set(settingsKey, initContext);
        }

        public static IDocumentStore GetDocumentStore<TStorageType>(ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            // First try to get a document store specific to a storage type (Subscriptions, Gateway, etc.)
            var initContext = settings.GetOrDefault<DocumentStoreInitContext>(featureSettingsKeys[typeof(TStorageType)]);

            // Next, if a connection string name exists for the storage type, create based on that
            if (initContext == null)
            {
                initContext = CreateStoreByConnectionStringName(settings, connStrKeys[typeof(TStorageType)]);
            }

            // Next try finding a shared DocumentStore
            if (initContext == null)
            {
                initContext = settings.GetOrDefault<DocumentStoreInitContext>(settingsKey);
            }

            // Otherwise, we need to create it ourselves, but do so only once.
            if (initContext == null)
            {
                // The holder is known to be non-null since we set it in SharedDocumentStore feature ctor
                var holder = settings.Get<SingleSharedDocumentStore>();
                if (holder.DocumentStoreProxy == null)
                {
                    holder.DocumentStoreProxy = CreateDefaultDocumentStore(settings);
                }

                initContext = holder.DocumentStoreProxy;
            }

            if (initContext == null)
            {
                throw new Exception($"RavenDB is configured as persistence for {typeof(TStorageType).Name} and no DocumentStore instance could be found.");
            }

            return initContext.Init(settings);
        }

        public static void SetCustomizeDocumentStoreDelegate(SettingsHolder settings, Action<IDocumentStore> customize)
        {
            settings.Set(CustomizeDocumentStoreKey, customize);
        }

        public static Action<IDocumentStore> GetCustomizeDocumentStoreDelegate(ReadOnlySettings settings)
        {
            return settings.GetOrDefault<Action<IDocumentStore>>(CustomizeDocumentStoreKey);
        }

        private static DocumentStoreInitContext CreateDefaultDocumentStore(ReadOnlySettings settings)
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

                return new DocumentStoreInitContext(storeByParams);
            }

            var initContext = CreateStoreByConnectionStringName(settings, "NServiceBus/Persistence/RavenDB", "NServiceBus/Persistence");

            if (initContext != null)
            {
                return initContext;
            }

            return CreateStoreByUrl(settings, "http://localhost:8080");
        }

        static DocumentStoreInitContext CreateStoreByConnectionStringName(ReadOnlySettings settings, params string[] connectionStringNames)
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

                return new DocumentStoreInitContext(docStore);
            }
            return null;
        }

        static DocumentStoreInitContext CreateStoreByUrl(ReadOnlySettings settings, string url)
        {
            var docStore = new DocumentStore
            {
                Url = url
            };

            if (docStore.DefaultDatabase == null)
            {
                docStore.DefaultDatabase = settings.EndpointName();
            }

            return new DocumentStoreInitContext(docStore);
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
    }
}
