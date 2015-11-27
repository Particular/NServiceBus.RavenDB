namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Raven.Abstractions.Commands;
    using Raven.Client;

    class OutboxRecordsCleaner
    {
        volatile bool doingCleanup;
        public IDocumentStore DocumentStore { get; set; }

        public async Task RemoveEntriesOlderThan(DateTime dateTime)
        {
            lock (this)
            {
                if (doingCleanup)
                {
                    return;
                }

                doingCleanup = true;
            }

            try
            {
                var deletionCommands = new List<ICommandData>();

                using (var session = DocumentStore.OpenAsyncSession())
                {
                    var query = session.Query<OutboxRecord, OutboxRecordsIndex>()
                        .Where(o => o.Dispatched)
                        .OrderBy(o => o.DispatchedAt);

                    using (var enumerator = await session.Advanced.StreamAsync(query))
                    {
                        while (await enumerator.MoveNextAsync())
                        {
                            if (enumerator.Current.Document.DispatchedAt >= dateTime)
                            {
                                break; // break streaming if we went past the threshold
                            }

                            deletionCommands.Add(new DeleteCommandData
                            {
                                Key = enumerator.Current.Key
                            });
                        }
                    }
                }

                await DocumentStore.AsyncDatabaseCommands.BatchAsync(deletionCommands);
            }
            finally
            {
                doingCleanup = false;
            }
        }
    }
}