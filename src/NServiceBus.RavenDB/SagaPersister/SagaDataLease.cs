namespace NServiceBus.Persistence.RavenDB
{
    using System;

    class SagaDataLease
    {
        public SagaDataLease(DateTime reservedUntil) => this.ReservedUntil = reservedUntil;

        public DateTime ReservedUntil { get; }
    }
}