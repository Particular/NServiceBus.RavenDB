namespace NServiceBus.Persistence.RavenDB
{
    using System.Linq;
    using NServiceBus.RavenDB.Outbox;
    using Raven.Client.Documents.Indexes;

    class OutboxRecordsIndex : AbstractIndexCreationTask<OutboxRecord>
    {
        public OutboxRecordsIndex()
        {
            Map = docs => from doc in docs
                select new
                {
                    doc.Dispatched,
                    doc.DispatchedAt
                };
        }
    }
}