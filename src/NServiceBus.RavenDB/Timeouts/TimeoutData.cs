namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System;
    using System.Collections.Generic;

    class TimeoutData
    {
        public TimeoutData()
        {
        }

        public TimeoutData(Timeout.Core.TimeoutData data)
        {
            Destination = data.Destination;
            SagaId = data.SagaId;
            State = data.State;
            Time = data.Time;
            OwningTimeoutManager = data.OwningTimeoutManager;
            Headers = data.Headers;
        }

        /// <summary>
        ///     Id of this timeout
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The address of the client who requested the timeout.
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        ///     The saga ID.
        /// </summary>
        public Guid SagaId { get; set; }

        /// <summary>
        ///     Additional state.
        /// </summary>
        public byte[] State { get; set; }

        /// <summary>
        ///     The time at which the timeout expires.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        ///     The timeout manager that owns this particular timeout
        /// </summary>
        public string OwningTimeoutManager { get; set; }

        /// <summary>
        ///     Store the headers to preserve them across timeouts
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        public Timeout.Core.TimeoutData ToCoreTimeoutData()
        {
            return new Timeout.Core.TimeoutData
            {
                Destination = Destination,
                Headers = Headers,
                OwningTimeoutManager = OwningTimeoutManager,
                SagaId = SagaId,
                State = State,
                Time = Time
            };
        }
    }
}
