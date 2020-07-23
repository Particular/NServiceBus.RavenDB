﻿namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Transport;
    using Raven.Client.Documents;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;

    public partial class PersistenceTestsConfiguration
    {
        DocumentStore documentStore;
        public bool SupportsDtc => true;
        public bool SupportsOutbox => false; //TODO configure and run outbox tests
        public bool SupportsFinders => true;
        public bool SupportsSubscriptions => true;
        public bool SupportsTimeouts => true;
        public bool SupportsPessimisticConcurrency => false;

        public ISagaIdGenerator SagaIdGenerator { get; private set; }
        public ISagaPersister SagaStorage { get; private set; }
        public ISynchronizedStorage SynchronizedStorage { get; private set; }
        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; }
        public IOutboxStorage OutboxStorage { get; }



        public Task Configure()
        {
            GetContextBagForSagaStorage = () =>
            {
                var context = new ContextBag();
                context.Set(new IncomingMessage("native id", new Dictionary<string, string>(), new byte[0]));
                return context;
            };

            SagaIdGenerator = new DefaultSagaIdGenerator();
            SagaStorage = new SagaPersister();

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
            SynchronizedStorage = new RavenDBSynchronizedStorage(sessionCreator);
            //OutboxStorage = new OutboxPersister(endpointName, sessionCreator, ttl);
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