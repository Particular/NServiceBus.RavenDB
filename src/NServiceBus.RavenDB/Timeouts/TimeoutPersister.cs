namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using NServiceBus.Logging;
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

        public TimeoutPersister()
        {
            TriggerCleanupEvery = TimeSpan.FromMinutes(2);
            CleanupGapFromTimeslice = TimeSpan.FromMinutes(1);
            shutdownTokenSource = new CancellationTokenSource();
        }

        readonly CancellationTokenSource shutdownTokenSource;
        readonly ILog logger = LogManager.GetLogger<TimeoutPersister>();
        
        public IDocumentStore DocumentStore { get; set; }
        public string EndpointName { get; set; }
        public TimeSpan CleanupGapFromTimeslice { get; set; }
        public TimeSpan TriggerCleanupEvery { get; set; }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeoutToExpire)
        {
            var now = DateTime.UtcNow;
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

            // default return value for when no results are found
            nextTimeoutToExpire = now.AddMinutes(10);

            using (var session = DocumentStore.OpenSession())
            {
                var query = GetChunkQuery(session)
                    .Where(t => t.Time > startSlice && t.Time <= now)
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
                        if (CancellationRequested())
                        {
                            return Enumerable.Empty<Tuple<string, DateTime>>();
                        }

                        results.Add(new Tuple<string, DateTime>(enumerator.Current.Document.Id, enumerator.Current.Document.Time));
                    }
                }

                var nextTimeout = GetChunkQuery(session)
                    .Where(t => t.Time > now)
                    .Take(1)
                    .FirstOrDefault();

                if (nextTimeout != null)
                {
                    nextTimeoutToExpire = nextTimeout.Time;
                }

                // Next execution is either now if we know we got stale results or at the start of the next chunk, otherwise we delay the next execution a bit
                else if (qhi.IsStale && results.Count == 0)
                {
                    nextTimeoutToExpire = now.AddSeconds(10);
                }
            }

            logger.Info($"Returning {results.Count} timeouts, next query at {nextTimeoutToExpire:O}");
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
                    .Take(1024) // RavenDB server default maximum page size
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

        bool CancellationRequested()
        {
            return shutdownTokenSource != null && shutdownTokenSource.IsCancellationRequested;
        }

        internal void Shutdown()
        {
            shutdownTokenSource.Cancel();
        }
    }
}