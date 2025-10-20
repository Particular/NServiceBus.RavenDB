namespace NServiceBus
{
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.RavenDB;

    /// <summary>
    /// Specifies the capabilities of the RavenDB suite of storages
    /// </summary>
    public partial class RavenDBPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<RavenDBPersistence>
    {
        // constructor parameter is a temporary workaround until the public constructor is removed
        RavenDBPersistence(object _)
        {
            Supports<StorageType.Sagas, RavenDbSagaStorage>();
            Supports<StorageType.Subscriptions, RavenDbSubscriptionStorage>();
            Supports<StorageType.Outbox, RavenDbOutboxStorage>();
        }

        static RavenDBPersistence IPersistenceDefinitionFactory<RavenDBPersistence>.Create() => new(null);
    }
}