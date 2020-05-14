namespace NServiceBus.Persistence.RavenDB
{
    using System;

    class SagaDataContainer
    {
        public string Id { get; set; }
        public string IdentityDocId { get; set; }
        public IContainSagaData Data { get; set; }

        internal static readonly Version SchemaVersion = new Version(1, 0, 0);
    }
}