namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Timeout.Core;
    using Raven.Abstractions.Commands;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Linq;

    class TimeoutPersister : IPersistTimeouts
    {
        public IDocumentStore DocumentStore { get; set; }
        public string EndpointName { get; set; }

        public TimeSpan CleanupGapFromTimeslice { get; set; }
        public TimeSpan TriggerCleanupEvery { get; set; }
        DateTime lastCleanupTime = DateTime.MinValue;
        bool seenStaleResults;

        public TimeoutPersister()
        {
            TriggerCleanupEvery = TimeSpan.FromMinutes(2);
            CleanupGapFromTimeslice = TimeSpan.FromMinutes(1);
        }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var now = DateTime.UtcNow;
            List<Tuple<string, DateTime>> results;

            // Allow for occasionally cleaning up old timeouts for edge cases where timeouts have been
            // added after startSlice have been set to a later timout and we might have missed them
            // because of stale indexes.
            if (seenStaleResults && 
                (TriggerCleanupEvery == TimeSpan.MinValue || lastCleanupTime.Add(TriggerCleanupEvery) > DateTime.UtcNow || lastCleanupTime == DateTime.MinValue))
            {
                results = DoCleanup(startSlice).ToList();
                lastCleanupTime = now;
                seenStaleResults = false;
            }
            else
            {
                results = new List<Tuple<string, DateTime>>();
            }

            using (var session = DocumentStore.OpenSession())
            {
                var query = session.Query<Timeout, TimeoutsIndex>()
                    .OrderBy(t => t.Time)
                    .Where(
                        t =>
                            t.OwningTimeoutManager == String.Empty ||
                            t.OwningTimeoutManager == EndpointName)
                    .Where(t => t.Time > startSlice)
                    .Select(t => new
                                 {
                                     t.Id,
                                     t.Time
                                 });

                QueryHeaderInformation qhi;
                using (var enumerator = session.Advanced.Stream(query, out qhi))
                {
                    // default return value for when no results are found and index is stale (non-stale is checked below)
                    nextTimeToRunQuery = now;

                    while (enumerator.MoveNext())
                    {
                        var dateTime = enumerator.Current.Document.Time;
                        nextTimeToRunQuery = dateTime; // since results are sorted on time asc, this will get the max time

                        if (dateTime > DateTime.UtcNow) break; // break on first future timeout

                        results.Add(new Tuple<string, DateTime>(enumerator.Current.Document.Id, dateTime));
                    }

                    // Next execution is either now if we haven't consumed the entire thing, or delayed
                    // a bit if we ded
                    if (qhi != null)
                    {
                        if (qhi.IsStable)
                        {
                            seenStaleResults = true;
                        }
                        else
                        {
                            // since we consumed all timeouts and we know the query returned non-stale results
                            nextTimeToRunQuery = nextTimeToRunQuery < DateTime.UtcNow.AddMinutes(10) ? nextTimeToRunQuery : DateTime.UtcNow.AddMinutes(10);                   
                        }
                    }
                }
            }

            return results;
        }

        public IEnumerable<Tuple<string, DateTime>> DoCleanup(DateTime startSlice)
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Advanced.AllowNonAuthoritativeInformation = true;

                var query = session.Query<Timeout, TimeoutsIndex>()
                    .OrderBy(t => t.Time)
                    .Where(
                        t =>
                            t.OwningTimeoutManager == String.Empty ||
                            t.OwningTimeoutManager == EndpointName)
                    ;

                return query
                    .Where(t => t.Time <= startSlice.Subtract(CleanupGapFromTimeslice))
                    .Select(t => new
                                 {
                                     t.Id,
                                     t.Time
                                 })
                    .Take(1024)
                    .ToList()
                    .Select(arg => new Tuple<string, DateTime>(arg.Id, arg.Time))
                    ;
            }
        }

        public void Add(TimeoutData timeout)
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new Timeout(timeout));
                session.SaveChanges();
            }
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
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

                timeoutData = timeout.ToTimeoutData();
                session.Delete(timeout);
                session.SaveChanges();
                return true;
            }
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var query = session.Query<Timeout, TimeoutsIndex>().Where(x => x.SagaId == sagaId).Select(x => x.Id);
                using (var enumerator = session.Advanced.Stream(query))
                {
                    while (enumerator.MoveNext())
                    {
                        session.Advanced.Defer(new DeleteCommandData{Key = enumerator.Current.Key});
                    }
                }
                session.SaveChanges();
            }
        }
    }
}