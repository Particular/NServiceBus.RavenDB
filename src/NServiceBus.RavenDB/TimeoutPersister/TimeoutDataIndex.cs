namespace NServiceBus.RavenDB.TimeoutPersister
{
    using System.Linq;
    using Raven.Client.Indexes;
    using Timeout.Core;

    class TimeoutDataIndex : AbstractIndexCreationTask<TimeoutData>
    {
        public TimeoutDataIndex()
        {
            Map = docs => from doc in docs
                select new
                       {
                           doc.Time,
                           doc.SagaId,
                           doc.OwningTimeoutManager,
                       };

            DisableInMemoryIndexing = true;
        }
    }
}
