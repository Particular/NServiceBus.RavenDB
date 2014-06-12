namespace NServiceBus.RavenDB
{
    using System;
    using Config.Conventions;
    using Internal;
    using NServiceBus;
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
            documentStore.ResourceManagerId = DefaultResourceManagerId();


            if (config.Settings.Get<bool>("Transactions.SuppressDistributedTransactions"))
            {
                documentStore.EnlistInDistributedTransactions = false;
            }

            return config;
        }

        static Guid DefaultResourceManagerId()
        {
            var resourceManagerId = Address.Local + "-" + EndpointHelper.GetEndpointVersion();

            return Helpers.DeterministicGuidBuilder(resourceManagerId);
        }
    }
}