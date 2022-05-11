namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Sagas;
    using NServiceBus.Transport;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;

    public partial class PersistenceTestsConfiguration
    {
        static PersistenceTestsConfiguration()
        {
            SagaVariants = new[]
            {
                new TestFixtureData(new TestVariant(new PersistenceConfiguration(useOptimisticConcurrency: true, useClusterWideTransactions: false))).SetArgDisplayNames("Optimistic", "NoClusterWideTx"),
                new TestFixtureData(new TestVariant(new PersistenceConfiguration(useOptimisticConcurrency: false, useClusterWideTransactions: false)) { SessionTimeout = TimeSpan.FromMilliseconds(2000) }).SetArgDisplayNames("Pessimistic", "NoClusterWideTx"),
                new TestFixtureData(new TestVariant(new PersistenceConfiguration(useOptimisticConcurrency: true, useClusterWideTransactions: true))).SetArgDisplayNames("Optimistic", "ClusterWideTx"),
                new TestFixtureData(new TestVariant(new PersistenceConfiguration(useOptimisticConcurrency: false, useClusterWideTransactions: true)) { SessionTimeout = TimeSpan.FromMilliseconds(2000) }).SetArgDisplayNames("Pessimistic", "ClusterWideTx"),
            };
            OutboxVariants = new[]
            {
                new TestFixtureData(new TestVariant(new PersistenceConfiguration(useOptimisticConcurrency: true, useClusterWideTransactions: false))).SetArgDisplayNames("Optimistic", "NoClusterWideTx"),
                new TestFixtureData(new TestVariant(new PersistenceConfiguration(useOptimisticConcurrency: true, useClusterWideTransactions: true))).SetArgDisplayNames("Optimistic", "ClusterWideTx")
            };
        }

        public class PersistenceConfiguration
        {
            public readonly bool UseOptimisticConcurrency;
            public readonly bool UseClusterWideTransactions;

            public PersistenceConfiguration(bool useOptimisticConcurrency, bool useClusterWideTransactions)
            {
                UseOptimisticConcurrency = useOptimisticConcurrency;
                UseClusterWideTransactions = useClusterWideTransactions;
            }
        }

        DocumentStore documentStore;

        public bool SupportsDtc => false;

        public bool SupportsOutbox => true;

        public bool SupportsFinders => false;

        public bool SupportsPessimisticConcurrency { get; private set; }

        public ISagaIdGenerator SagaIdGenerator { get; private set; }

        public ISagaPersister SagaStorage { get; private set; }

        public IOutboxStorage OutboxStorage { get; private set; }

        public Func<ICompletableSynchronizedStorageSession> CreateStorageSession { get; private set; }

        public async Task Configure(CancellationToken cancellationToken = default)
        {
            GetContextBagForOutbox = GetContextBagForSagaStorage = () =>
            {
                var context = new ContextBag();
                context.Set(new IncomingMessage("native id", new Dictionary<string, string>(), Array.Empty<byte>()));
                return context;
            };

            var persistenceConfiguration = (PersistenceConfiguration)Variant.Values[0];

            SagaIdGenerator = new DefaultSagaIdGenerator();
            var sagaPersistenceConfiguration = new SagaPersistenceConfiguration();
            if (persistenceConfiguration.UseOptimisticConcurrency)
            {
                sagaPersistenceConfiguration.UseOptimisticLocking();
            }
            SupportsPessimisticConcurrency = sagaPersistenceConfiguration.EnablePessimisticLocking;
            if (SessionTimeout.HasValue)
            {
                sagaPersistenceConfiguration.SetPessimisticLeaseLockAcquisitionTimeout(SessionTimeout.Value);
            }
            SagaStorage = new SagaPersister(sagaPersistenceConfiguration, persistenceConfiguration.UseClusterWideTransactions);

            var dbName = Guid.NewGuid().ToString();
            string urls = persistenceConfiguration.UseClusterWideTransactions
                ? Environment.GetEnvironmentVariable("CommaSeparatedRavenClusterUrls") ?? "http://localhost:8081,http://localhost:8082,http://localhost:8083"
                : Environment.GetEnvironmentVariable("RavenSingleNodeUrl") ?? "http://localhost:8080";
            documentStore = new DocumentStore
            {
                Urls = urls.Split(','),
                Database = dbName
            };
            documentStore.Initialize();
            var dbRecord = new DatabaseRecord(dbName);
            await documentStore.Maintenance.Server.SendAsync(new CreateDatabaseOperation(dbRecord), cancellationToken);

            IOpenTenantAwareRavenSessions sessionCreator = new OpenRavenSessionByDatabaseName(new DocumentStoreWrapper(documentStore), persistenceConfiguration.UseClusterWideTransactions);
            CreateStorageSession = () => new RavenDBSynchronizedStorageSession(sessionCreator);

            OutboxStorage = new OutboxPersister(documentStore.Database, sessionCreator, RavenDbOutboxStorage.DeduplicationDataTTLDefault, persistenceConfiguration.UseClusterWideTransactions);
        }

        public async Task Cleanup(CancellationToken cancellationToken = default)
        {
            // Periodically the delete will throw an exception because Raven has the database locked
            // To solve this we have a retry loop with a delay
            var triesLeft = 3;

            while (triesLeft-- > 0)
            {
                try
                {
                    await documentStore.Maintenance.Server.SendAsync(new DeleteDatabasesOperation(documentStore.Database, hardDelete: true), cancellationToken);
                    documentStore.Dispose();
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    if (triesLeft == 0)
                    {
                        throw;
                    }

                    await Task.Delay(250, cancellationToken);
                }
            }
        }

    }
}