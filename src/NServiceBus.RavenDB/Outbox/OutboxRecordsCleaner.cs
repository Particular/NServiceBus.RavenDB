namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Raven.Abstractions.Commands;
    using Raven.Abstractions.Data;
    using Raven.Client;

    class OutboxRecordsCleaner
    {
        public IDocumentStore DocumentStore { get; set; }

        volatile bool doingCleanup;

        public void RemoveEntriesOlderThan(DateTime dateTime)
        {
            lock (this)
            {
                if (doingCleanup)
                    return;

                doingCleanup = true;
            }

            try
            {
                var deletionCommands = new List<ICommandData>();

                using (var session = DocumentStore.OpenSession())
                {
                    var query = session.Query<OutboxRecord, OutboxRecordsIndex>()
                        .Where(o => o.Dispatched)
                        .OrderBy(o => o.DispatchedAt);

                    QueryHeaderInformation qhi;
                    using (var enumerator = session.Advanced.Stream(query, out qhi))
                    {
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current.Document.DispatchedAt >= dateTime)
                                break; // break streaming if we went past the threshold

                            deletionCommands.Add(new DeleteCommandData { Key = enumerator.Current.Key });
                        }
                    }
                }

                DocumentStore.DatabaseCommands.Batch(deletionCommands);;
            }
            finally
            {
                doingCleanup = false;
            }
        }

    }
}