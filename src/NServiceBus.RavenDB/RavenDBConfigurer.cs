namespace NServiceBus.RavenDB
{
    using System.Collections.Generic;
    using Features;
    using NServiceBus.Persistence;
    using SessionManagement;
    using RavenLogManager = Raven.Abstractions.Logging.LogManager;

    class RavenDBConfigurer : IConfigurePersistence<RavenDB>
    {
        public void Enable(Configure config, List<Storage> storagesToEnable)
        {
            RavenLogManager.CurrentLogManager = new NoOpLogManager();

            config.Settings.EnableFeatureByDefault<RavenDbStorageSession>();
            config.Settings.EnableFeatureByDefault<SharedDocumentStore>();

            if (storagesToEnable.Contains(Storage.Timeouts))
                config.Settings.EnableFeatureByDefault<RavenDbTimeoutStorage>();

            if (storagesToEnable.Contains(Storage.Sagas)) 
                config.Settings.EnableFeatureByDefault<RavenDbSagaStorage>();
            
            if (storagesToEnable.Contains(Storage.Subscriptions)) 
                config.Settings.EnableFeatureByDefault<RavenDbSubscriptionStorage>();

            if (storagesToEnable.Contains(Storage.GatewayDeduplication))
                config.Settings.EnableFeatureByDefault<RavenDbGatewayDeduplication>();            
        }
    }
}
