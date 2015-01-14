using System;
using System.Collections.Generic;

namespace NServiceBus.RavenDB.Outbox
{
    class OutboxRecord
    {
        public string MessageId { get; set; }
        public bool Dispatched { get; set; }
        public DateTime? DispatchedAt { get; set; }
        public IList<OutboxOperation> TransportOperations { get; set; }

        public class OutboxOperation
        {
            public string MessageId { get; set; }
            public byte[] Message { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public Dictionary<string, string> Options { get; set; }
        }
    }
}
