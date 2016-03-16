namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using NServiceBus.RavenDB.Shutdown;
    using NServiceBus.Timeout.Core;
    using Raven.Abstractions.Data;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;
    using Raven.Client.Linq;
    using CoreTimeoutData = NServiceBus.Timeout.Core.TimeoutData;
    using Timeout = TimeoutData;

    class TimeoutPersister : IPersistTimeouts, IPersistTimeoutsV2
    {
        DateTime lastCleanupTime = DateTime.MinValue;

        public TimeoutPersister(IRegisterShutdownDelegates shutdownRegistry)
        {
            TriggerCleanupEvery = TimeSpan.FromMinutes(2);
            CleanupGapFromTimeslice = TimeSpan.FromMinutes(1);
            shutdownTokenSource = new CancellationTokenSource();
            shutdownRegistry.Register(() => shutdownTokenSource.Cancel());
        }

        /// <summary>
        /// RavenDB server default maximum page size 
        /// </summary>
        private int maximumPageSize = 1024;

        private CancellationTokenSource shutdownTokenSource;
        public IDocumentStore DocumentStore { get; set; }
        public string EndpointName { get; set; }
        public TimeSpan CleanupGapFromTimeslice { get; set; }
        public TimeSpan TriggerCleanupEvery { get; set; }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var now = DateTime.UtcNow;
            var defaultNextPollTime = DateTime.UtcNow.AddMinutes(10);

            nextTimeToRunQuery = defaultNextPollTime;

            List<Tuple<string, DateTime>> results;

            // Allow for occasionally cleaning up old timeouts for edge cases where timeouts have been
            // added after startSlice have been set to a later timeout and we might have missed them
            // because of stale indexes.
            if (lastCleanupTime == DateTime.MinValue || lastCleanupTime.Add(TriggerCleanupEvery) < now)
            {
                results = GetCleanupChunk(startSlice).ToList();
            }
            else
            {
                results = new List<Tuple<string, DateTime>>();
            }

            RavenQueryStatistics statistics;

            using (var session = DocumentStore.OpenSession())
            {
                int totalCount, skipCount = 0;

                do
                {
                    if (CancellationRequested())
                    {
                        return Enumerable.Empty<Tuple<string, DateTime>>();
                    }

                    var query = GetChunkQuery(session);

                    var dueTimeouts =
                        query.Statistics(out statistics)
                            .Where(t => t.Time >= startSlice)
                            .Where(t => t.Time < now)
                            .Skip(skipCount)
                            .Select(t => new
                            {
                                t.Id,
                                t.Time
                            })
                            .Take(maximumPageSize);

                    foreach (var dueTimeout in dueTimeouts)
                    {
                        results.Add(new Tuple<string, DateTime>(dueTimeout.Id, dueTimeout.Time));
                    }

                    if (CancellationRequested())
                    {
                        return Enumerable.Empty<Tuple<string, DateTime>>();
                    }

                    skipCount = results.Count + statistics.SkippedResults;
                    totalCount = statistics.TotalResults;
                }
                while (results.Count < totalCount);

                var nextDueTimeout =
                    GetChunkQuery(session)
                        .FirstOrDefault(t => t.Time > now);

                if (nextDueTimeout != null)
                {
                    nextTimeToRunQuery = nextDueTimeout.Time;
                }
            }

            // Next execution is either now if we know we got stale results or at the start of the next chunk, otherwise we delay the next execution a bit
            if (statistics.IsStale && results.Count == 0)
            {
                nextTimeToRunQuery = now;
            }

            return results;
        }

        public void Add(CoreTimeoutData timeout)
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new Timeout(timeout));
                session.SaveChanges();
            }
        }

        public bool TryRemove(string timeoutId, out CoreTimeoutData timeoutData)
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var timeout = session.Load<Timeout>(timeoutId);
                if (timeout == null)
                {
                    timeoutData = null;
                    return false;
                }

                timeoutData = timeout.ToCoreTimeoutData();
                session.Delete(timeout);
                session.SaveChanges();
                return true;
            }
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            var operation = DocumentStore.DatabaseCommands.DeleteByIndex("TimeoutsIndex", new IndexQuery { Query = string.Format("SagaId:{0}", sagaId) }, new BulkOperationOptions { AllowStale = true });
            operation.WaitForCompletion();
        }

        IRavenQueryable<Timeout> GetChunkQuery(IDocumentSession session)
        {
            session.Advanced.AllowNonAuthoritativeInformation = true;
            session.Advanced.NonAuthoritativeInformationTimeout = TimeSpan.FromSeconds(10);
            return session.Query<Timeout, TimeoutsIndex>()
                .OrderBy(t => t.Time)
                .Where(
                    t =>
                        t.OwningTimeoutManager == String.Empty ||
                        t.OwningTimeoutManager == EndpointName);
        }

        public IEnumerable<Tuple<string, DateTime>> GetCleanupChunk(DateTime startSlice)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var chunk = GetChunkQuery(session)
                    .Where(t => t.Time <= startSlice.Subtract(CleanupGapFromTimeslice))
                    .Select(t => new
                    {
                        t.Id,
                        t.Time
                    })
                    .Take(maximumPageSize)
                    .ToList()
                    .Select(arg => new Tuple<string, DateTime>(arg.Id, arg.Time));

                lastCleanupTime = DateTime.UtcNow;

                return chunk;
            }
        }

        public CoreTimeoutData Peek(string timeoutId)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var timeoutData = session.Load<Timeout>(timeoutId);
                if (timeoutData != null)
                {
                    return timeoutData.ToCoreTimeoutData();
                }

                return null;
            }
        }

        public bool TryRemove(string timeoutId)
        {
            try
            {
                CoreTimeoutData timeoutData;
                return TryRemove(timeoutId, out timeoutData);
            }
            catch (ConcurrencyException)
            {
                return false;
            }
        }
        
        private bool CancellationRequested()
        {
            return shutdownTokenSource != null && shutdownTokenSource.IsCancellationRequested;
        }

    }
}