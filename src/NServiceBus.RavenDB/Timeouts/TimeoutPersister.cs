namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Timeout.Core;
    using Raven.Abstractions.Commands;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Linq;

    class TimeoutPersister : IPersistTimeouts
    {
        public IDocumentStore DocumentStore { get; set; }

        public string EndpointName { get; set; }

        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var results = new List<Tuple<string, DateTime>>();
            using (var session = OpenSession())
            {
                var query = session.Query<Timeout, TimeoutsIndex>()
                    .Where(
                        t =>
                            t.OwningTimeoutManager == String.Empty ||
                            t.OwningTimeoutManager == EndpointName)
                    .Where(t => t.Time > startSlice)
                    .OrderBy(t => t.Time)
                    .Select(t => t.Time);

                QueryHeaderInformation qhi;
                using (var enumerator = session.Advanced.Stream(query, out qhi))
                {
                    // default return value for when no results are found and index is stale (non-stale is checked below)
                    nextTimeToRunQuery = qhi.IndexTimestamp;

                    while (enumerator.MoveNext())
                    {
                        var dateTime = enumerator.Current.Document;
                        nextTimeToRunQuery = dateTime; // since results are sorted on time asc, this will get the max time

                        if (dateTime > DateTime.UtcNow) return results; // break on first future timeout

                        results.Add(new Tuple<string, DateTime>(enumerator.Current.Key, enumerator.Current.Document));
                    }

                    if (!qhi.IsStable) nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(10); // since we consumed all timeouts and no future timeouts found
                }
            }

            return results;
        }

        public void Add(TimeoutData timeout)
        {
            using (var session = OpenSession())
            {
                session.Store(new Timeout(timeout));
                session.SaveChanges();
            }
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            using (var session = OpenSession())
            {
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
            using (var session = OpenSession())
            {
                var query = session.Query<Timeout>().Where(x => x.SagaId == sagaId).Select(x => x.Id);
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

        IDocumentSession OpenSession()
        {
            var session = DocumentStore.OpenSession();
            session.Advanced.UseOptimisticConcurrency = true;
            return session;
        }
    }
}