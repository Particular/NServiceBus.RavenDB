namespace NServiceBus.Persistence
{
    /// <summary>
    /// Specifies the capabilities of the ravendb suite of storages
    /// </summary>
    public class RavenDB : PersistenceDefinition
    {
        public RavenDB()
        {
            Supports(Storage.GatewayDeduplication);
            Supports(Storage.Timeouts);
            Supports(Storage.Sagas);
            Supports(Storage.Subscriptions);
        }
    }
}
