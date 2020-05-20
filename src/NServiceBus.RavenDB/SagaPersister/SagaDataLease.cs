namespace NServiceBus.Persistence.RavenDB
{
    using System;

    class SagaDataLease
    {
        public DateTime? ReservedUntil { get; set; }
    }
}