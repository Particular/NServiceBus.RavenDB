namespace NServiceBus.Persistence.RavenDB
{
    using System;

    class SagaDataLease
    {
        public string LeaseId { get; set; }
        public DateTime? ReservedUntil { get; set; }
    }
}