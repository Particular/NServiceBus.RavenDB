namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Timeout.Core;
    using Raven.Abstractions.Data;
    using Raven.Abstractions.Extensions;
    using Raven.Client;
    using Raven.Client.Linq;

    class QueryTimeouts : IQueryTimeouts
    {
        DateTime lastCleanupTime = DateTime.MinValue;
        readonly IDocumentStore documentStore;
        bool abort = false;

        public QueryTimeouts(IDocumentStore store)
        {
            documentStore = store;
            TriggerCleanupEvery = TimeSpan.FromMinutes(2);
            CleanupGapFromTimeslice = TimeSpan.FromMinutes(1);
        }

        public string EndpointName { get; set; }
        public TimeSpan CleanupGapFromTimeslice { get; set; }
        public TimeSpan TriggerCleanupEvery { get; set; }

        public async Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
        {
            if(abort)
            {
                return new TimeoutsChunk(new TimeoutsChunk.Timeout[ 0 ], DateTime.UtcNow.AddHours(1));
            }

            var now = DateTime.UtcNow;
            List<TimeoutsChunk.Timeout> results;

            // Allow for occasionally cleaning up old timeouts for edge cases where timeouts have been
            // added after startSlice have been set to a later timout and we might have missed them
            // because of stale indexes.
            if(lastCleanupTime == DateTime.MinValue || lastCleanupTime.Add(TriggerCleanupEvery) < now)
            {
                results = (await GetCleanupChunk(startSlice)).ToList();
            }
            else
            {
                results = new List<TimeoutsChunk.Timeout>();
            }

            // default return value for when no results are found
            var nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(10);

            using(var session = documentStore.OpenAsyncSession())
            {
                var query = GetChunkQuery(session)
                    .Where(t => t.Time > startSlice && t.Time <= DateTime.UtcNow)
                    .Select(t => new
                    {
                        t.Id,
                        t.Time
                    });

                var qhi = new Reference<QueryHeaderInformation>();
                using(var enumerator = await session.Advanced.StreamAsync(query, qhi))
                {
                    while(await enumerator.MoveNextAsync())
                    {
                        if(abort)
                        {
                            break;
                        }

                        var dateTime = enumerator.Current.Document.Time;
                        nextTimeToRunQuery = dateTime; // since results are sorted on time asc, this will get the max time < now

                        results.Add(new TimeoutsChunk.Timeout(enumerator.Current.Document.Id, dateTime));
                    }
                }

                // Next execution is either now if we know we got stale results or at the start of the next chunk, otherwise we delay the next execution a bit
                if(qhi.Value != null && qhi.Value.IsStale && results.Count == 0)
                {
                    nextTimeToRunQuery = now;
                }
            }

            return new TimeoutsChunk(results, nextTimeToRunQuery);
        }

        public async Task<IEnumerable<TimeoutsChunk.Timeout>> GetCleanupChunk(DateTime startSlice)
        {
            if(abort)
            {
                return new TimeoutsChunk.Timeout[0];
            }

            using(var session = documentStore.OpenAsyncSession())
            {
                var query = await GetChunkQuery(session)
                    .Where(t => t.Time <= startSlice.Subtract(CleanupGapFromTimeslice))
                    .Select(t => new
                    {
                        t.Id,
                        t.Time
                    })
                    .Take(1024)
                    .ToListAsync();

                var chunk = query.Select(arg => new TimeoutsChunk.Timeout(arg.Id, arg.Time));

                lastCleanupTime = DateTime.UtcNow;

                return chunk;
            }
        }

        IRavenQueryable<TimeoutData> GetChunkQuery(IAsyncDocumentSession session)
        {
            session.Advanced.AllowNonAuthoritativeInformation = true;
            return session.Query<TimeoutData, TimeoutsIndex>()
                .OrderBy(t => t.Time)
                .Where(
                    t =>
                        t.OwningTimeoutManager == string.Empty ||
                        t.OwningTimeoutManager == EndpointName);
        }

        internal void Shutdown()
        {
            abort = true;
        }
    }
}