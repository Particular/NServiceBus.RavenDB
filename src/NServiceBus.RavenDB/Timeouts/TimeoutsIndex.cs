namespace NServiceBus.Persistence.RavenDB
{
    using System.Linq;
    using NServiceBus.TimeoutPersisters.RavenDB;
    using Raven.Client.Documents.Indexes;

    class TimeoutsIndex : AbstractIndexCreationTask<TimeoutData>
    {
        public TimeoutsIndex()
        {
            Map = docs => from doc in docs
                select new
                {
                    doc.Time,
                    doc.SagaId,
                    doc.OwningTimeoutManager
                };
        }
    }
}