namespace NServiceBus.RavenDB
{
    using Gateway.Deduplication;
    using Gateway.Persistence;

    public static class ConfigureGateway
    {
        public static Configure RunGatewayWithRavenPersistence(this Configure config)
        {
            return config.RunGateway(typeof(RavenPersistence));
        }


        /// <summary>
        /// Use RavenDB messages persistence by the gateway.
        /// </summary>
        public static Configure UseRavenGatewayPersister(this Configure config)
        {
            config.ThrowIfStoreNotConfigured();

            config.Configurer.ConfigureComponent<RavenPersistence>(DependencyLifecycle.SingleInstance);
            return config;
        }

        /// <summary>
        /// Use RavenDB for message deduplication by the gateway.
        /// </summary>
        public static Configure UseRavenGatewayDeduplication(this Configure config)
        {
            config.ThrowIfStoreNotConfigured();

            config.Configurer.ConfigureComponent<RavenDeduplication>(DependencyLifecycle.SingleInstance);
            return config;
        }
    }
}