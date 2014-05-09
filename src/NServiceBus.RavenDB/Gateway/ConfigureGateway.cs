namespace NServiceBus.RavenDB
{
    using System;

    public static class ConfigureGateway
    {
        /// <summary>
        /// Use RavenDB for message deduplication by the gateway.
        /// </summary>
        public static Configure UseRavenDBGatewayDeduplicationStorage(this Configure config)
        {
            throw new NotImplementedException("The Gateway functionality is not implemented here anymore");
        }
    }
}