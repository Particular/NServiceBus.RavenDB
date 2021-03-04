﻿namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Sagas;
    using NServiceBus.Transport;
    using Raven.Client.Documents;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;

    public partial class PersistenceTestsConfiguration
    {
        static PersistenceTestsConfiguration()
        {
            var optimisticConcurrencyConfiguration = new SagaPersistenceConfiguration();
            optimisticConcurrencyConfiguration.UseOptimisticLocking();
            var pessimisticLockingConfiguration = new SagaPersistenceConfiguration();

            SagaVariants = new[]
            {
                new TestVariant(optimisticConcurrencyConfiguration),
                new TestVariant(pessimisticLockingConfiguration)
            };
            OutboxVariants = new[]
            {
                new TestVariant(optimisticConcurrencyConfiguration)
            };
        }

        DocumentStore documentStore;

        public bool SupportsDtc => false;

        public bool SupportsOutbox => true;

        public bool SupportsFinders => false;

        public bool SupportsPessimisticConcurrency { get; private set; }

        public ISagaIdGenerator SagaIdGenerator { get; private set; }

        public ISagaPersister SagaStorage { get; private set; }

        public ISynchronizedStorage SynchronizedStorage { get; private set; }

        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; private set; }

        public IOutboxStorage OutboxStorage { get; private set; }

        public Task Configure()
        {
            GetContextBagForOutbox = GetContextBagForSagaStorage = () =>
            {
                var context = new ContextBag();
                context.Set(new IncomingMessage("native id", new Dictionary<string, string>(), new byte[0]));
                return context;
            };

            SagaIdGenerator = new DefaultSagaIdGenerator();
            var sagaPersistenceConfiguration = Variant.Values[0] as SagaPersistenceConfiguration;
            SupportsPessimisticConcurrency = sagaPersistenceConfiguration.EnablePessimisticLocking;
            if (SessionTimeout.HasValue)
            {
                sagaPersistenceConfiguration.SetPessimisticLeaseLockAcquisitionTimeout(SessionTimeout.Value);
            }
            SagaStorage = new SagaPersister(sagaPersistenceConfiguration);

            var dbName = Guid.NewGuid().ToString();
            var urls = Environment.GetEnvironmentVariable("CommaSeparatedRavenClusterUrls") ?? "http://localhost:8080";
            documentStore = new DocumentStore
            {
                Urls = urls.Split(','),
                Database = dbName
            };
            documentStore.Initialize();
            var dbRecord = new DatabaseRecord(dbName);
            documentStore.Maintenance.Server.Send(new CreateDatabaseOperation(dbRecord));

            IOpenTenantAwareRavenSessions sessionCreator = new OpenRavenSessionByDatabaseName(new DocumentStoreWrapper(documentStore));
            SynchronizedStorage = new RavenDBSynchronizedStorage(sessionCreator, null);
            SynchronizedStorageAdapter = new RavenDBSynchronizedStorageAdapter(null);

            OutboxStorage = new OutboxPersister(documentStore.Database, sessionCreator, RavenDbOutboxStorage.DeduplicationDataTTLDefault);
            return Task.CompletedTask;
        }

        public async Task Cleanup()
        {
            // Periodically the delete will throw an exception because Raven has the database locked
            // To solve this we have a retry loop with a delay
            var triesLeft = 3;

            while (triesLeft-- > 0)
            {
                try
                {
                    documentStore.Maintenance.Server.Send(new DeleteDatabasesOperation(documentStore.Database, hardDelete: true));
                    documentStore.Dispose();
                    break;
                }
                catch
                {
                    if (triesLeft == 0)
                    {
                        throw;
                    }

                    await Task.Delay(250);
                }
            }
        }
    }
}