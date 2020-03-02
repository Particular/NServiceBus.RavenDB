namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Collections.Generic;

    class LegacyTimeoutData
    {
        /// <summary>
        ///     Id of this timeout
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The address of the client who requested the timeout.
        /// </summary>
        public LegacyAddress Destination { get; set; }

        public Guid SagaId { get; set; }

        public byte[] State { get; set; }

        public DateTime Time { get; set; }

        public string OwningTimeoutManager { get; set; }

        public Dictionary<string, string> Headers { get; set; }
    }
}