namespace NServiceBus.RavenDB
{
    using Gateway.Deduplication;

    public static class ConfigureGateway
    {
        /// <summary>
        /// Use RavenDB for message deduplication by the gateway.
        /// </summary>
        public static Configure UseRavenDBGatewayDeduplicationStorage(this Configure config)
        {
            config.ThrowIfStoreNotConfigured();

            config.Configurer.ConfigureComponent<RavenDeduplication>(DependencyLifecycle.SingleInstance);
            return config;
        }
    }
}