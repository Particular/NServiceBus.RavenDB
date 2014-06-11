namespace NServiceBus.RavenDB
{
    using NServiceBus;
    using Persistence;
    using Raven.Client.Document;

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
            documentStore.ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId(config);


            if (config.Settings.Get<bool>("Transactions.SuppressDistributedTransactions"))
            {
                documentStore.EnlistInDistributedTransactions = false;
            }

            return config;
        }
    }
}