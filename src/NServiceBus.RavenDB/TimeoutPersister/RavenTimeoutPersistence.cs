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

        public RavenTimeoutPersistence(StoreAccessor storeAccessor)
        {
            store = storeAccessor.Store;
        }

        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            try
            {
                var results = new List<Tuple<string, DateTime>>();
                using (var session = OpenSession())
                {
                    var now = DateTime.UtcNow;
                    var strippedNow = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);

                    var query = session.Query<TimeoutData>()
                        .Where(
                            t =>
                                t.OwningTimeoutManager == String.Empty ||
                                t.OwningTimeoutManager == Configure.EndpointName)
                        .Where(t => t.Time > startSlice && t.Time <= strippedNow)
                        .OrderBy(t => t.Time)
                        .Select(t => t.Time);

                    QueryHeaderInformation qhi;
                    using (var enumerator = session.Advanced.Stream(query, out qhi))
                    {
                        if (qhi.TotalResults == 0)
                        {
                            nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(10);
                        }
                        else
                        {
                            while (enumerator.MoveNext())
                            {
                                results.Add(new Tuple<string, DateTime>(enumerator.Current.Key, enumerator.Current.Document));
                            }
                            
                            nextTimeToRunQuery = results.Max(x => x.Item2);
                            if (nextTimeToRunQuery < qhi.IndexTimestamp) nextTimeToRunQuery = qhi.IndexTimestamp;
                        }
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

                timeoutData.Time = DateTime.UtcNow.AddYears(-1);
                session.SaveChanges();

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