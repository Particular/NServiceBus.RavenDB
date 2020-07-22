namespace NServiceBus.Persistence.RavenDB
{
    using System.Collections.Generic;

    class SagaDataLeaseHolder
    {
        //TODO: quick and dirty tuple for now
        public List<(string DocumentId, long Index)> DocumentsIdsAndIndexes { get; } = new List<(string, long)>();
    }
}