namespace NServiceBus.RavenDB
{
    using System;
    using NServiceBus;
    using Persistence;
    using Raven.Client;
    using Raven.Client.Document;
    using RavenLogManager = Raven.Abstractions.Logging.LogManager;

    /// <summary>
    /// Extension methods to configure RavenDB persister.
    /// </summary>
    static class ConfigureRavenPersistence
    {
        private static void SetupRavenPersistence(Configure config, IDocumentStore documentStore)
        {
            if (!config.Configurer.HasComponent<IDocumentStore>())
            {
                config.Configurer.ConfigureComponent(() => documentStore, DependencyLifecycle.SingleInstance);
                RavenUserInstaller.RunInstaller = true; // TODO this smells
            }
            else
            {
                // TODO if this is not acceptable, we are going to need a type to docStore mapping set up
                var configuredStore = config.Builder.Build<IDocumentStore>();
                if (configuredStore != documentStore)
                    throw new Exception("You can only point to one RavenDB document store for all persisted types");
            }
        }

        /// <summary>
        /// Apply the NServiceBus conventions to a <see cref="DocumentStore"/> .
        /// </summary>
        static Configure ApplyRavenDBConventions(this Configure config, DocumentStore documentStore)
        {
            if (documentStore.Url == null)
            {
                documentStore.Url = RavenPersistenceConstants.GetDefaultUrl(config);
            }
            if (documentStore.DefaultDatabase == null)
            {
                documentStore.DefaultDatabase = config.Settings.EndpointName();
            }
            documentStore.ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId(config);


            if (config.Settings.Get<bool>("Transactions.SuppressDistributedTransactions"))
            {
                documentStore.EnlistInDistributedTransactions = false;
            }
            RavenLogManager.CurrentLogManager = new NoOpLogManager();

            return config;
        }
    }
}