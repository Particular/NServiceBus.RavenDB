namespace NServiceBus
{
    using System;
    using NServiceBus.Settings;
    using Raven.Client.Documents;

    /// <summary>
    /// Configuration settings specific to the timeout storage
    /// </summary>
    public static class RavenDbGatewayDeduplicationSettingsExtensions
    {
        /// <summary>
        /// Configures the given document store to be used when storing gateway deduplication data
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="documentStore">The document store to use</param>
        [ObsoleteEx(
            Message = "RavenDB gateway persistence has been moved to the NServiceBus.Gateway.RavenDB dedicated package.",
            RemoveInVersion = "8.0.0",
            TreatAsErrorFromVersion = "7.0.0")]
        public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForGatewayDeduplication(this PersistenceExtensions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Configures the given document store to be used when storing gateway deduplication data
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        [ObsoleteEx(
            Message = "RavenDB gateway persistence has been moved to the NServiceBus.Gateway.RavenDB dedicated package.",
            RemoveInVersion = "8.0.0",
            TreatAsErrorFromVersion = "7.0.0")]
        public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForGatewayDeduplication(this PersistenceExtensions<RavenDBPersistence> cfg, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Configures the given document store to be used when storing gateway deduplication data
        /// </summary>
        /// <param name="cfg">The persistence configuration object</param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        [ObsoleteEx(
            Message = "RavenDB gateway persistence has been moved to the NServiceBus.Gateway.RavenDB dedicated package.",
            RemoveInVersion = "8.0.0",
            TreatAsErrorFromVersion = "7.0.0")]
        public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForGatewayDeduplication(this PersistenceExtensions<RavenDBPersistence> cfg, Func<ReadOnlySettings, IServiceProvider, IDocumentStore> storeCreator)
        {
            throw new NotImplementedException();
        }
    }
}