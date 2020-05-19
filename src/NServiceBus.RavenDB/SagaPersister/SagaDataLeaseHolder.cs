namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;

    class SagaDataLeaseHolder
    {
        // quick and dirty tuple for now
        public List<Tuple<string, long>> NamesAndIndex { get; set; } = new List<Tuple<string, long>>();
    }
}