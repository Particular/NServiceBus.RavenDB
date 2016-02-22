﻿namespace NServiceBus.RavenDB.Outbox
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

                using (var session = DocumentStore.OpenSession())
                {
                    var query = session.Query<OutboxRecord, OutboxRecordsIndex>()
                        .Where(o => o.Dispatched)
                        .OrderBy(o => o.DispatchedAt);

                    using (var enumerator = session.Advanced.Stream(query))
                    {
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current.Document.DispatchedAt >= dateTime)
                            {
                                break; // break streaming if we went past the threshold
                            }

                            /*
                             * For some unknown reasons when streaming classes with no
                             * Id property the Key is not filled. The document Id can be
                             * found in the streamed document metadata.
                             */
                            var key = enumerator.Current.Key ?? enumerator.Current.Metadata["@id"].ToString();

                            deletionCommands.Add(new DeleteCommandData
                            {
                                Key = key
                            });
                        }
                    }
                }

                await DocumentStore.AsyncDatabaseCommands.BatchAsync(deletionCommands).ConfigureAwait(false);
            }
            finally
            {
                doingCleanup = false;
            }
        }
    }
}