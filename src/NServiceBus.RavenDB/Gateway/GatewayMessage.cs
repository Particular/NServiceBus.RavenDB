namespace NServiceBus.Gateway.Deduplication
{
    using System;

    /// <summary>
    ///     The Gateway message
    /// </summary>
    [ObsoleteEx(Message = "This type was not meant to be used in external code is being made internal.", RemoveInVersion = "5", TreatAsErrorFromVersion = "4")]
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