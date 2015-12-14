namespace NServiceBus.Persistence
{
    using NServiceBus.Features;
    using NServiceBus.RavenDB;
    using NServiceBus.RavenDB.Outbox;
    using NServiceBus.RavenDB.SessionManagement;
    using RavenLogManager = Raven.Abstractions.Logging.LogManager;

    /// <summary>
    ///     Specifies the capabilities of the ravendb suite of storages
    /// </summary>
    public class RavenDBPersistence : PersistenceDefinition
    {

        /// <summary>
        ///     Defines the capabilities
        /// </summary>
        public RavenDBPersistence()
        {
            Defaults(s =>
            {
                RavenLogManager.CurrentLogManager = new NoOpLogManager();

                s.EnableFeatureByDefault<RavenDbStorageSession>();
                s.EnableFeatureByDefault<SharedDocumentStore>();
                s.EnableFeatureByDefault<SynchronizedStorage>();
            });

            Supports<StorageType.GatewayDeduplication>(s => s.EnableFeatureByDefault<RavenDbGatewayDeduplication>());
            Supports<StorageType.Timeouts>(s => s.EnableFeatureByDefault<RavenDbTimeoutStorage>());
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<RavenDbSagaStorage>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<RavenDbSubscriptionStorage>());
            Supports<StorageType.Outbox>(s => s.EnableFeatureByDefault<RavenDbOutboxStorage>());
        }
    }
}