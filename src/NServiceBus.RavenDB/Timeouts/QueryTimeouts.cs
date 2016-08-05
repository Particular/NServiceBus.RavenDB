namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Timeout.Core;
    using Raven.Abstractions.Data;
    using Raven.Abstractions.Extensions;
    using Raven.Client;
    using Raven.Client.Linq;
    using TimeoutData = NServiceBus.Timeout.Core.TimeoutData;

    class QueryTimeouts : IQueryTimeouts
    {
        static TimeoutsChunk.Timeout[] EmptyTimeouts = new TimeoutsChunk.Timeout[0];

        public QueryTimeouts(IDocumentStore documentStore, string endpointName)
        {
            this.documentStore = documentStore;
            this.endpointName = endpointName;
            TriggerCleanupEvery = TimeSpan.FromMinutes(2);
            CleanupGapFromTimeslice = TimeSpan.FromMinutes(1);
            shutdownTokenSource = new CancellationTokenSource();
            logger = LogManager.GetLogger<QueryTimeouts>();
        }

        public TimeSpan CleanupGapFromTimeslice { get; set; }
        public TimeSpan TriggerCleanupEvery { get; set; }
        public Func<DateTime> GetUtcNow { get; set; } = () => DateTime.UtcNow;

        public async Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
        {
            var now = GetUtcNow();
            List<TimeoutsChunk.Timeout> results;

            // Allow for occasionally cleaning up old timeouts for edge cases where timeouts have been
            // added after startSlice have been set to a later timout and we might have missed them
            // because of stale indexes.
            if (lastCleanupTime == DateTime.MinValue || lastCleanupTime.Add(TriggerCleanupEvery) < now)
            {
                results = await GetCleanupChunk(now).ConfigureAwait(false);
            }
            else
            {
                results = new List<TimeoutsChunk.Timeout>();
            }

            // default return value for when no results are found
            var nextTimeoutToExpire = now.AddMinutes(10);

            using (var session = documentStore.OpenAsyncSession())
            {
                // This is all an unexecuted Raven query expression
                var query = GetChunkQuery(session)
                    .Where(t => t.Time > startSlice && t.Time <= now)
                    .Select(to => new { to.Id, to.Time }); // Must be anonymous type so Raven server can understand

                var qhi = new Reference<QueryHeaderInformation>();
                using (var enumerator = await session.Advanced.StreamAsync(query, qhi).ConfigureAwait(false))
                {
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        if (CancellationRequested())
                        {
                            return new TimeoutsChunk(EmptyTimeouts, nextTimeoutToExpire);
                        }

                        results.Add(new TimeoutsChunk.Timeout(enumerator.Current.Document.Id, enumerator.Current.Document.Time));
                    }
                }

                var nextTimeout = await GetChunkQuery(session)
                        .Where(t => t.Time > now)
                        .Take(1)
                        .Select(to => new { to.Time }) // Must be anonymous type so Raven server can understand
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);

                if (nextTimeout != null)
                {
                    nextTimeoutToExpire = nextTimeout.Time;
                }

                // Next execution is either now if we know we got stale results or at the start of the next chunk, otherwise we delay the next execution a bit
                else if (qhi.Value.IsStale && results.Count == 0)
                {
                    nextTimeoutToExpire = now.AddSeconds(10);
                }
            }

            logger.DebugFormat("Returning {0} timeouts, next due at {1:O}", results.Count, nextTimeoutToExpire);
            return new TimeoutsChunk(results.ToArray(), nextTimeoutToExpire);
        }

        public async Task<List<TimeoutsChunk.Timeout>> GetCleanupChunk(DateTime fromTime)
        {
            var cutoff = fromTime.Subtract(CleanupGapFromTimeslice);

            using (var session = documentStore.OpenAsyncSession())
            {
                var query = await GetChunkQuery(session)
                    .Where(t => t.Time <= cutoff)
                    .Select(t => new
                    {
                        t.Id,
                        t.Time
                    })
                    .Take(maximumPageSize)
                    .ToListAsync()
                    .ConfigureAwait(false);

                var chunk = query.Select(arg => new TimeoutsChunk.Timeout(arg.Id, arg.Time)).ToList();

                lastCleanupTime = DateTime.UtcNow;

                return chunk;
            }
        }

        public void Shutdown()
        {
            shutdownTokenSource.Cancel();
        }

        IRavenQueryable<TimeoutData> GetChunkQuery(IAsyncDocumentSession session)
        {
            session.Advanced.AllowNonAuthoritativeInformation = true;
            return session.Query<TimeoutData, TimeoutsIndex>()
                .OrderBy(t => t.Time)
                .Where(
                    t =>
                        t.OwningTimeoutManager == string.Empty ||
                        t.OwningTimeoutManager == endpointName);
        }

        bool CancellationRequested()
        {
            return shutdownTokenSource != null && shutdownTokenSource.IsCancellationRequested;
        }

        string endpointName;
        DateTime lastCleanupTime = DateTime.MinValue;
        IDocumentStore documentStore;

        /// <summary>
        /// RavenDB server default maximum page size 
        /// </summary>
        private int maximumPageSize = 1024;
        CancellationTokenSource shutdownTokenSource;
        ILog logger;
    }
}