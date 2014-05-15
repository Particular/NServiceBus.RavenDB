namespace NServiceBus.RavenDB.TimeoutPersister
{
    using System.Linq;
    using Raven.Client.Indexes;

    class TimeoutsIndex : AbstractIndexCreationTask<Timeout>
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
