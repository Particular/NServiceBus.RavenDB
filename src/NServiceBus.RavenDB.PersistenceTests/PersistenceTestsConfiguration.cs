namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
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
    using Settings;

    public partial class PersistenceTestsConfiguration
    {
        static PersistenceTestsConfiguration()
        {
            var optimisticConcurrencyConfiguration = new SagaPersistenceConfiguration();
            optimisticConcurrencyConfiguration.UseOptimisticLocking();
            var pessimisticLockingConfiguration = new SagaPersistenceConfiguration();

            var doNotClusterWideTx = new PersistenceExtensions<RavenDBPersistence>(new SettingsHolder());
            var useClusterWideTx = new PersistenceExtensions<RavenDBPersistence>(new SettingsHolder());
            useClusterWideTx.EnableClusterWideTransactions();

            SagaVariants = new[]
            {
                new TestFixtureData(new TestVariant(optimisticConcurrencyConfiguration, doNotClusterWideTx)).SetArgDisplayNames("Optimistic", "NoClusterWideTx"),
                new TestFixtureData(new TestVariant(pessimisticLockingConfiguration, doNotClusterWideTx)).SetArgDisplayNames("Pessimistic", "NoClusterWideTx"),
                new TestFixtureData(new TestVariant(optimisticConcurrencyConfiguration, useClusterWideTx)).SetArgDisplayNames("Optimistic", "ClusterWideTx"),
                new TestFixtureData(new TestVariant(pessimisticLockingConfiguration, useClusterWideTx)).SetArgDisplayNames("Pessimistic", "ClusterWideTx"),
            };
            OutboxVariants = new[]
            {
                new TestFixtureData(new TestVariant(optimisticConcurrencyConfiguration, doNotClusterWideTx)).SetArgDisplayNames("Optimistic", "NoClusterWideTx"),
                new TestFixtureData(new TestVariant(optimisticConcurrencyConfiguration, useClusterWideTx)).SetArgDisplayNames("Optimistic", "ClusterWideTx")
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

        public async Task Configure(CancellationToken cancellationToken = default)
        {
            GetContextBagForOutbox = GetContextBagForSagaStorage = () =>
            {
                var context = new ContextBag();
                context.Set(new IncomingMessage("native id", new Dictionary<string, string>(), new byte[0]));
                return context;
            };

            var ravenConfiguration = Variant.Values[1] as PersistenceExtensions<RavenDBPersistence>;
            var settings = ravenConfiguration.GetSettings();
            var useClusterWideTx = settings.GetOrDefault<bool>(RavenDbStorageSession.UseClusterWideTransactions);

            SagaIdGenerator = new DefaultSagaIdGenerator();
            var sagaPersistenceConfiguration = Variant.Values[0] as SagaPersistenceConfiguration;
            SupportsPessimisticConcurrency = sagaPersistenceConfiguration.EnablePessimisticLocking;
            if (SessionTimeout.HasValue)
            {
                sagaPersistenceConfiguration.SetPessimisticLeaseLockAcquisitionTimeout(SessionTimeout.Value);
            }
            SagaStorage = new SagaPersister(sagaPersistenceConfiguration, useClusterWideTx);

            var dbName = Guid.NewGuid().ToString();
            string urls = useClusterWideTx
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

            IOpenTenantAwareRavenSessions sessionCreator = new OpenRavenSessionByDatabaseName(new DocumentStoreWrapper(documentStore), useClusterWideTx);
            SynchronizedStorage = new RavenDBSynchronizedStorage(sessionCreator, null);
            SynchronizedStorageAdapter = new RavenDBSynchronizedStorageAdapter(null);

            OutboxStorage = new OutboxPersister(documentStore.Database, sessionCreator, RavenDbOutboxStorage.DeduplicationDataTTLDefault, useClusterWideTx);
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
                catch (Exception ex) when (!(ex is OperationCanceledException) || !cancellationToken.IsCancellationRequested)
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