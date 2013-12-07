namespace NServiceBus.RavenDB
{
    using System;
    using Config;
    using NServiceBus;
    using NServiceBus.Gateway.Deduplication;
    using NServiceBus.Gateway.Persistence;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.Saga;
    using NServiceBus.Settings;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Raven.Client;
    using Raven.Client.Document;

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
        /// <remarks>This method does not use any of the NServiceBus conventions either specified or out of the box.</remarks>
        /// <param name="config">The configuration object.</param>
        /// <param name="documentStore">An <see cref="IDocumentStore"/>.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RavenDBPersistence(this Configure config, DocumentStore documentStore)
        {
            documentStore.Conventions.FindTypeTagName = RavenConventions.FindTypeTagName;
            documentStore.Conventions.MaxNumberOfRequestsPerSession = 100;

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
            documentStore.Initialize();
            config.Configurer.ConfigureComponent(() => new StoreAccessor(documentStore), DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RavenSessionFactory>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RavenUnitOfWork>(DependencyLifecycle.InstancePerUnitOfWork);

            Raven.Abstractions.Logging.LogManager.CurrentLogManager = new NoOpLogManager();

            RavenUserInstaller.RunInstaller = true;

            return config;
        }

        /// <summary>
        /// Specifies the mapping to use for when resolving the database name to use for each message.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="convention">The method referenced by a Func delegate for finding the database name for the specified message.</param>
        /// <returns>The configuration object.</returns>
        public static Configure MessageToDatabaseMappingConvention(this Configure config, Func<IMessageContext, string> convention)
        {
            RavenSessionFactory.GetDatabaseName = convention;

            return config;
        }

        public static void RegisterDefaults(this Configure config, DocumentStore documentStore)
        {
            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => config.RavenDBPersistence(documentStore));
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => config.UseRavenTimeoutPersister());
            InfrastructureServices.SetDefaultFor<IPersistMessages>(() => config.UseRavenGatewayPersister());
            InfrastructureServices.SetDefaultFor<IDeduplicateMessages>(() => config.UseRavenGatewayDeduplication());
            InfrastructureServices.SetDefaultFor<ISubscriptionStorage>(() => config.RavenSubscriptionStorage());
        }

    }
}