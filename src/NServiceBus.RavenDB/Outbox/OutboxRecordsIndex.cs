namespace NServiceBus.RavenDB.Outbox
{
    using System.Linq;
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
                    doc.DispatchedAt,
                };

            DisableInMemoryIndexing = true;
        }
    }
}
