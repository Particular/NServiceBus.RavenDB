namespace NServiceBus.Gateway.Deduplication
{
    using System;

    class GatewayMessage
    {
        public string Id { get; set; }
        public DateTime TimeReceived { get; set; }
    }
}