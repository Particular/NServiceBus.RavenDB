namespace NServiceBus.Persistence.RavenDB
{
    using System.Collections.Generic;

    class SagaDataLeaseHolder
    {
        public ICollection<(string DocumentId, long Index)> DocumentsIdsAndIndexes { get; } = new List<(string, long)>();
    }
}