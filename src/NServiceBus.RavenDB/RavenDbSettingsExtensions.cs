namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;

    /// <summary>
    /// Provides configurations methods for the Raven storage
    /// </summary>
    public static class RavenDbSettingsExtensions
    {
        /// <summary>
        /// Configures the storage to use the given document store supplied
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="documentStore">Document store managed by me as a user</param>
        public static PersistenceExtensions<RavenDBPersistence> SetDefaultDocumentStore(this PersistenceExtensions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDefaultStore(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        /// Configures the storages to use the given document store supplied
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        public static PersistenceExtensions<RavenDBPersistence> SetDefaultDocumentStore(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IReadOnlySettings, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDefaultStore(cfg.GetSettings(), storeCreator);
            return cfg;
        }

        /// <summary>
        /// Configures the storages to use the given document store supplied
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        public static PersistenceExtensions<RavenDBPersistence> SetDefaultDocumentStore(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IReadOnlySettings, IServiceProvider, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDefaultStore(cfg.GetSettings(), storeCreator);
            return cfg;
        }

        /// <summary>
        /// Specifies the async session that the shared persisters (saga + outbox) should use.
        /// The lifecycle is controlled by me
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="getAsyncSessionFunc">A func returning the async session to be used</param>
        public static PersistenceExtensions<RavenDBPersistence> UseSharedAsyncSession(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IDictionary<string, string>, IAsyncDocumentSession> getAsyncSessionFunc)
        {
            if (getAsyncSessionFunc == null)
            {
                throw new ArgumentNullException(nameof(getAsyncSessionFunc));
            }

            cfg.GetSettings().Set(RavenDbStorageSession.SharedAsyncSession, getAsyncSessionFunc);
            return cfg;
        }

        /// <summary>
        /// Specifies the mapping to use for when resolving the database name to use for each message.
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="convention">
        /// The method referenced by a Func delegate for finding the database name for the specified message.
        /// </param>
        public static PersistenceExtensions<RavenDBPersistence> SetMessageToDatabaseMappingConvention(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IDictionary<string, string>, string> convention)
        {
            cfg.GetSettings().Set(RavenDbStorageSession.MessageToDatabaseMappingConvention, convention);
            return cfg;
        }

        /// <summary>
        /// Obtains the saga persistence configuration options.
        /// </summary>
        public static SagaPersistenceConfiguration Sagas(this PersistenceExtensions<RavenDBPersistence> cfg)
        {
            return cfg.GetSettings().GetOrCreate<SagaPersistenceConfiguration>();
        }

        /// <summary>
        /// Configures the persistence to make use of cluster wide transactions.
        /// </summary>
        public static PersistenceExtensions<RavenDBPersistence> EnableClusterWideTransactions(
            this PersistenceExtensions<RavenDBPersistence> config)
        {
            config.GetSettings().Set(RavenDbStorageSession.UseClusterWideTransactions, true);
            return config;
        }
    }
}