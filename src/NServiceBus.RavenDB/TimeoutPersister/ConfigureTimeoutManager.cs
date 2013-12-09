namespace NServiceBus.RavenDB
{
    using NServiceBus.RavenDB.Persistence.TimeoutPersister;

    static class ConfigureTimeoutManager
    {

        /// <summary>
        /// Use the Raven timeout persister implementation.
        /// </summary>
        public static Configure UseRavenTimeoutPersister(this Configure config)
        {
            config.ThrowIfStoreNotConfigured();

            config.Configurer.ConfigureComponent<RavenTimeoutPersistence>(DependencyLifecycle.SingleInstance);

            return config;
        }

    }
}