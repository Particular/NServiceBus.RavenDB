namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System.Linq;
    using Raven.Client.Indexes;

    class TimeoutsIndex : AbstractIndexCreationTask<TimeoutData>
    {
        public TimeoutsIndex()
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
