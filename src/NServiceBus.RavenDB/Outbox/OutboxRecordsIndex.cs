namespace NServiceBus.Persistence.RavenDB
{
    using System.Linq;
    using NServiceBus.RavenDB.Outbox;
    using Raven.Client.Indexes;

    class OutboxRecordsIndex : AbstractIndexCreationTask<OutboxRecord>
    {
        public OutboxRecordsIndex()
        {
            Map = docs => from doc in docs
                select new
                {
                    doc.MessageId,
                    doc.Dispatched,
                    doc.DispatchedAt
                };

            DisableInMemoryIndexing = true;
        }
    }
}