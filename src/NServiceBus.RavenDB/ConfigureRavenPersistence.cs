namespace NServiceBus.RavenDB
{
    using NServiceBus;
    using Persistence;
    using Raven.Client.Document;
    using RavenLogManager = Raven.Abstractions.Logging.LogManager;

    /// <summary>
    /// Extension methods to configure RavenDB persister.
    /// </summary>
    static class ConfigureRavenPersistence
    {
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