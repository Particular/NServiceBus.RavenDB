namespace NServiceBus.RavenDB
{
    using Features;
    using NServiceBus.Persistence;
    using SessionManagement;
    using RavenLogManager = Raven.Abstractions.Logging.LogManager;

    class RavenDBConfigurer : IConfigurePersistence<RavenDB>
    {
        public void Enable(Configure config)
        {
            config.Settings.EnableFeatureByDefault<RavenDbTimeoutStorage>();
            config.Settings.EnableFeatureByDefault<RavenDbSagaStorage>();
            config.Settings.EnableFeatureByDefault<RavenDbStorageSession>();
            config.Settings.EnableFeatureByDefault<RavenDbSubscriptionStorage>();
            config.Settings.EnableFeatureByDefault<SharedDocumentStore>();
            RavenLogManager.CurrentLogManager = new NoOpLogManager();
        }
    }
}
