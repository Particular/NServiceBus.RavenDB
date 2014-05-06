namespace NServiceBus.RavenDB
{
    using System;
    using NServiceBus;
    using Persistence;
    using Settings;
    using Raven.Client;
    using Raven.Client.Document;
    using RavenLogManager = Raven.Abstractions.Logging.LogManager;

    /// <summary>
    /// Extension methods to configure RavenDB persister.
    /// </summary>
    public static class ConfigureRavenPersistence
    {

        internal static void ThrowIfStoreNotConfigured(this Configure config)
        {
            if (!config.Configurer.HasComponent<StoreAccessor>())
            {
                throw new Exception(string.Format("Call {0}.RavenPersistence(Configure, DocumentStore) first.", typeof(ConfigureRavenPersistence).Name));
            }
        }

        /// <summary>
        /// Configures RavenDB as the default persistence.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="documentStore">An <see cref="DocumentStore"/>.</param>
        /// <param name="applyConventions"><code>true</code> to call <see cref="RavenDBStorageApplyConventions"/> on <paramref name="documentStore"/>.</param>
        /// <returns>The instance passed in by <paramref name="config"/> to enable the fluent API.</returns>
        public static Configure RavenDBStorage(this Configure config, DocumentStore documentStore, bool applyConventions)
        {
            if (applyConventions)
            {
                RavenDBStorageApplyConventions(documentStore);
            }

            config.Configurer.ConfigureComponent(() => new StoreAccessor(documentStore), DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RavenSessionFactory>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RavenUnitOfWork>(DependencyLifecycle.InstancePerUnitOfWork);


            RavenUserInstaller.RunInstaller = true;

            return config;
        }

        public static Configure RavenDBStorageWithSelfManagedSession(this Configure config, DocumentStore documentStore, bool applyConventions,Func<IDocumentSession> sessionProvider)
        {
            if (applyConventions)
            {
                RavenDBStorageApplyConventions(documentStore);
            }

            config.Configurer.ConfigureComponent(() => new StoreAccessor(documentStore), DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<ISessionProvider>(()=>new UserControlledSessionProvider(sessionProvider),DependencyLifecycle.InstancePerCall);
            
            RavenUserInstaller.RunInstaller = true;

            return config;
        }

        /// <summary>
        /// Specifies the mapping to use for when resolving the database name to use for each message.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="convention">The method referenced by a Func delegate for finding the database name for the specified message.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RavenDBStorageMessageToDatabaseMappingConvention(this Configure config, Func<IMessageContext, string> convention)
        {
            RavenSessionFactory.GetDatabaseName = convention;

            return config;
        }


        /// <summary>
        /// Apply the NServiceBus conventions to a <see cref="DocumentStore"/> .
        /// </summary>
        public static void RavenDBStorageApplyConventions(DocumentStore documentStore)
        {
            if (documentStore.Url == null)
            {
                documentStore.Url = RavenPersistenceConstants.DefaultUrl;
            }
            if (documentStore.DefaultDatabase == null)
            {
                documentStore.DefaultDatabase = Configure.EndpointName;
            }
            documentStore.ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId;


            if (SettingsHolder.Get<bool>("Transactions.SuppressDistributedTransactions"))
            {
                documentStore.EnlistInDistributedTransactions = false;
            }
            RavenLogManager.CurrentLogManager = new NoOpLogManager();
        }

        public static void RavenDBStorageAsDefault(this Configure config, DocumentStore documentStore)
        {
            config.RavenDBStorage(documentStore, true);
            config.UseRavenDBTimeoutStorage();
            config.UseRavenDBSagaStorage();
            config.UseRavenDBGatewayDeduplicationStorage();
            config.UseRavenDBSubscriptionStorage();
        }

    }
}