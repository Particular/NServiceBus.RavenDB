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
            SynchronizedStorage = new RavenDBSynchronizedStorage(new OpenRavenSessionByDatabaseName(new DocumentStoreWrapper(store)));
            SynchronizedStorageAdapter = new RavenDBSynchronizedStorageAdapter();

            GetContextBagForSagaStorage = () =>
            {
                var contextBag = new ContextBag();
                contextBag.Set(new IncomingMessage(Guid.NewGuid().ToString("N"), new Dictionary<string, string>(0), new byte[0]));
                return contextBag;
            };
        }

        public bool SupportsDtc { get; } = true;
        public bool SupportsOutbox { get; } = false;
        public bool SupportsFinders { get; } = false;
        public bool SupportsSubscriptions { get; } = false;
        public bool SupportsTimeouts { get; } = false;
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