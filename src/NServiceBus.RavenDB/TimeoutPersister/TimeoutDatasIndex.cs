namespace NServiceBus.RavenDB.TimeoutPersister
{
    using System.Linq;
    using Raven.Client.Indexes;
    using Timeout.Core;

    public class TimeoutDatasIndex : AbstractIndexCreationTask<TimeoutData>
    {
        public TimeoutDatasIndex()
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
