namespace NServiceBus.RavenDB.Persistence.TimeoutPersister
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using Raven.Abstractions.Commands;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Linq;
    using Logging;
    using Timeout.Core;

    class RavenTimeoutPersistence : IPersistTimeouts
    {
        readonly IDocumentStore store;

        public RavenTimeoutPersistence(IDocumentStore documentStore)
        {
            store = documentStore;
        }

        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            try
            {
                var results = new List<Tuple<string, DateTime>>();
                using (var session = OpenSession())
                {
                    var query = session.Query<TimeoutData>()
                        .Where(
                            t =>
                                t.OwningTimeoutManager == String.Empty ||
                                t.OwningTimeoutManager == Configure.EndpointName)
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
            catch (WebException ex)
            {
                LogRavenConnectionFailure(ex);
                throw;
            }
        }

        public void Add(TimeoutData timeout)
        {
            using (var session = OpenSession())
            {
                session.Store(timeout);
                session.SaveChanges();
            }
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            using (var session = OpenSession())
            {
                timeoutData = session.Load<TimeoutData>(timeoutId);

                if (timeoutData == null)
                    return false;

                session.Delete(timeoutData);
                session.SaveChanges();

                return true;
            }
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            using (var session = OpenSession())
            {
                var query = session.Query<TimeoutData>().Where(x => x.SagaId == sagaId).Select(x => x.Id);
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
            var session = store.OpenSession();
            session.Advanced.UseOptimisticConcurrency = true;
            return session;
        }

        void LogRavenConnectionFailure(Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Raven could not be contacted. We tried to access Raven using the following url: {0}.",
                store.Url);
            sb.AppendLine();
            sb.AppendFormat("Please ensure that you can open the Raven Studio by navigating to {0}.", store.Url);
            sb.AppendLine();
            sb.AppendLine(
                @"To configure NServiceBus to use a different Raven connection string add a connection string named ""NServiceBus.Persistence"" in your config file, example:");
            sb.AppendFormat(
                @"<connectionStrings>
    <add name=""NServiceBus.Persistence"" connectionString=""http://localhost:9090"" />
</connectionStrings>");
            sb.AppendLine("Original exception: " + exception);

            Logger.Warn(sb.ToString());
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(RavenTimeoutPersistence));
    }
}