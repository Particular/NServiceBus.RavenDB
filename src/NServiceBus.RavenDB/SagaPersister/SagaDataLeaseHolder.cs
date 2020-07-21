namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;

    class SagaDataLeaseHolder
    {
        //TODO: quick and dirty tuple for now
        public List<Tuple<string, long>> DocumentsIdsAndIndexes { get; } = new List<Tuple<string, long>>();
    }
}