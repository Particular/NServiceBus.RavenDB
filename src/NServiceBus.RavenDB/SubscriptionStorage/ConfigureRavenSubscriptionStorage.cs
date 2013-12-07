namespace NServiceBus.RavenDB
{
    using Persistence.SubscriptionStorage;

    public static class ConfigureRavenSubscriptionStorage
    {
        public static Configure RavenSubscriptionStorage(this Configure config)
        {
            config.ThrowIfStoreNotConfigured();

            config.Configurer.ConfigureComponent<RavenSubscriptionStorage>(DependencyLifecycle.SingleInstance);

            return config;
        }
    }
}