namespace NServiceBus.RavenDB.Gateway.Persistence
{
    using System;
    using System.Collections.Generic;

    class GatewayMessage
    {
        public IDictionary<string, string> Headers { get; set; }

        public DateTime TimeReceived { get; set; }

        public string Id { get; set; }

        public byte[] OriginalMessage { get; set; }

        public bool Acknowledged { get; set; }
    }
}