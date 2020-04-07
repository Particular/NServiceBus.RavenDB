namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Tests;
    using NServiceBus.Sagas;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transport;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations.Indexes;

    public partial class PersistenceTestsConfiguration
    {
        IDocumentStore store;
        ReusableDB db;

        //TODO implement transaction timeout configuration
        public PersistenceTestsConfiguration(TimeSpan? transactionTimeout = null)
        {
            db = new ReusableDB();
            store = db.NewStore();
            store.Initialize();

            SagaStorage = new SagaPersister();
            SagaIdGenerator = new DefaultSagaIdGenerator();
            var sessionCreator = new OpenRavenSessionByDatabaseName(new DocumentStoreWrapper(store));
            SynchronizedStorage = new RavenDBSynchronizedStorage(sessionCreator);
            SynchronizedStorageAdapter = new RavenDBSynchronizedStorageAdapter();
            OutboxStorage = new OutboxPersister("outbox-tests", sessionCreator);
            TimeoutStorage = new TimeoutPersister(store);
            //TODO owning timeout property not set when storing timeouts via persister
            TimeoutQuery = new QueryTimeouts(store, "");
            // Taken from DocumentStoreInitializer
            foreach (var index in new[] { new TimeoutsIndex() })
            {
                try
                {
                    index.Execute(store);
                }
                catch (Exception) // Apparently ArgumentException can be thrown as well as a WebException; not taking any chances
                {
                    var getIndexOp = new GetIndexOperation(index.IndexName);

                    var existingIndex = store.Maintenance.Send(getIndexOp);
                    if (existingIndex == null || !index.CreateIndexDefinition().Equals(existingIndex))
                        throw;
                }
            }

            SubscriptionStorage = new SubscriptionPersister(store);

            // Configure incoming message on context required to create tenant-aware document sessions:
            GetContextBagForOutbox =
            GetContextBagForSagaStorage = () =>
            {
                var contextBag = new ContextBag();
                contextBag.Set(new IncomingMessage(Guid.NewGuid().ToString("N"), new Dictionary<string, string>(0), new byte[0]));
                return contextBag;
            };
        }

        public bool SupportsDtc { get; } = true;
        public bool SupportsOutbox { get; } = true;
        public bool SupportsFinders { get; } = false;
        public bool SupportsSubscriptions { get; } = true;
        public bool SupportsTimeouts { get; } = true;
        public bool SupportsPessimisticConcurrency { get; } = false;
        public ISagaIdGenerator SagaIdGenerator { get; }
        public ISagaPersister SagaStorage { get; }
        public ISynchronizedStorage SynchronizedStorage { get; }
        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; }
        public ISubscriptionStorage SubscriptionStorage { get; }
        public IPersistTimeouts TimeoutStorage { get; }
        public IQueryTimeouts TimeoutQuery { get; }
        public IOutboxStorage OutboxStorage { get; }

        public Task Configure()
        {
            return Task.CompletedTask;
        }

        public Task Cleanup()
        {
            store.Dispose();
            db.Dispose();
            return Task.CompletedTask;
        }

        public Task CleanupMessagesOlderThan(DateTimeOffset beforeStore)
        {
            return Task.CompletedTask;
        }
    }
}