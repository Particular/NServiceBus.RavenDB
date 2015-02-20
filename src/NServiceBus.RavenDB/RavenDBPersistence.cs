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
            });

            Supports(Storage.GatewayDeduplication, s => s.EnableFeatureByDefault<RavenDbGatewayDeduplication>());
            Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<RavenDbTimeoutStorage>());
            Supports(Storage.Sagas, s => s.EnableFeatureByDefault<RavenDbSagaStorage>());
            Supports(Storage.Subscriptions, s => s.EnableFeatureByDefault<RavenDbSubscriptionStorage>());
            Supports(Storage.Outbox, s => s.EnableFeatureByDefault<RavenDbOutboxStorage>());
        }
    }
}