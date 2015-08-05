namespace NServiceBus.TimeoutPersisters.RavenDB
{
    using System;
    using NServiceBus.Timeout.Core;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using CoreTimeoutData = Timeout.Core.TimeoutData;
    using Timeout = TimeoutData;

    class TimeoutPersister : IPersistTimeouts
    {
        readonly IDocumentStore documentStore;

        public TimeoutPersister(IDocumentStore store)
        {
            documentStore = store;
        }

        public void Add(CoreTimeoutData timeout, TimeoutPersistenceOptions options)
        {
            using (var session = documentStore.OpenSession())
            {
                session.Store(new Timeout(timeout));
                session.SaveChanges();
            }
        }

        public bool TryRemove(string timeoutId, TimeoutPersistenceOptions options, out CoreTimeoutData timeoutData)
        {
            using (var session = documentStore.OpenSession())
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

        public void RemoveTimeoutBy(Guid sagaId, TimeoutPersistenceOptions options)
        {
            var operation = documentStore.DatabaseCommands.DeleteByIndex("TimeoutsIndex", new IndexQuery { Query = string.Format("SagaId:{0}", sagaId) }, new BulkOperationOptions { AllowStale = true });
            operation.WaitForCompletion();
        }
    }
}