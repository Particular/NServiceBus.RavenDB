namespace NServiceBus.Gateway.Deduplication
{
    using System;

    /// <summary>
    ///     The Gateway message
    /// </summary>
    public class GatewayMessage
    {
        /// <summary>
        ///     Id of this message.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The time at which the message was received.
        /// </summary>
        public DateTime TimeReceived { get; set; }
    }
}