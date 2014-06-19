namespace NServiceBus.RavenDB
{
    using System;

    /// <summary>
    /// Configures the gateway
    /// </summary>
    public static class ConfigureGateway
    {
        /// <summary>
        /// Use RavenDB for message deduplication by the gateway.
        /// </summary>
// ReSharper disable once UnusedParameter.Global
        public static Configure UseRavenDBGatewayDeduplicationStorage(this Configure config)
        {
            throw new NotImplementedException("The Gateway functionality is not implemented here anymore");
        }
    }
}