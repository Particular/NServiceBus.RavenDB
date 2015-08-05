namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Timeout.Core;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Linq;

    class QueryTimeouts : IQueryTimeouts
    {
        DateTime lastCleanupTime = DateTime.MinValue;
        readonly IDocumentStore documentStore;

        public QueryTimeouts(IDocumentStore store)
        {
            documentStore = store;
            TriggerCleanupEvery = TimeSpan.FromMinutes(2);
            CleanupGapFromTimeslice = TimeSpan.FromMinutes(1);
        }

        public string EndpointName { get; set; }
        public TimeSpan CleanupGapFromTimeslice { get; set; }
        public TimeSpan TriggerCleanupEvery { get; set; }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var now = DateTime.UtcNow;
            List<Tuple<string, DateTime>> results;

            // Allow for occasionally cleaning up old timeouts for edge cases where timeouts have been
            // added after startSlice have been set to a later timout and we might have missed them
            // because of stale indexes.
            if (lastCleanupTime == DateTime.MinValue || lastCleanupTime.Add(TriggerCleanupEvery) < now)
            {
                results = GetCleanupChunk(startSlice).ToList();
            }
            else
            {
                results = new List<Tuple<string, DateTime>>();
            }

            // default return value for when no results are found
            nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(10);

            using (var session = documentStore.OpenSession())
            {
                var query = GetChunkQuery(session)
                    .Where(t => t.Time > startSlice)
                    .Select(t => new
                    {
                        t.Id,
                        t.Time
                    });

                QueryHeaderInformation qhi;
                using (var enumerator = session.Advanced.Stream(query, out qhi))
                {
                    while (enumerator.MoveNext())
                    {
                        var dateTime = enumerator.Current.Document.Time;
                        nextTimeToRunQuery = dateTime; // since results are sorted on time asc, this will get the max time < now

                        if (dateTime > DateTime.UtcNow)
                        {
                            break; // break on first future timeout
                        }

                        results.Add(new Tuple<string, DateTime>(enumerator.Current.Document.Id, dateTime));
                    }
                }

                // Next execution is either now if we know we got stale results or at the start of the next chunk, otherwise we delay the next execution a bit
                if (qhi != null && qhi.IsStale && results.Count == 0)
                {
                    nextTimeToRunQuery = now;
                }
            }

            return results;
        }

        public IEnumerable<Tuple<string, DateTime>> GetCleanupChunk(DateTime startSlice)
        {
            using (var session = documentStore.OpenSession())
            {
                var chunk = GetChunkQuery(session)
                    .Where(t => t.Time <= startSlice.Subtract(CleanupGapFromTimeslice))
                    .Select(t => new
                    {
                        t.Id,
                        t.Time
                    })
                    .Take(1024)
                    .ToList()
                    .Select(arg => new Tuple<string, DateTime>(arg.Id, arg.Time));

                lastCleanupTime = DateTime.UtcNow;

                return chunk;
            }
        }

        IRavenQueryable<TimeoutData> GetChunkQuery(IDocumentSession session)
        {
            session.Advanced.AllowNonAuthoritativeInformation = true;
            return session.Query<TimeoutData, TimeoutsIndex>()
                .OrderBy(t => t.Time)
                .Where(
                    t =>
                        t.OwningTimeoutManager == string.Empty ||
                        t.OwningTimeoutManager == EndpointName);
        }
    }
}