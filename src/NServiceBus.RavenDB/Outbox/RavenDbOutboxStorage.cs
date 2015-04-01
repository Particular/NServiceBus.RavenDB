namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using System.Threading;
    using NServiceBus.Features;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.Settings;
    using Raven.Client;

    class RavenDbOutboxStorage : Feature
    {
        public RavenDbOutboxStorage()
        {
            DependsOn<Outbox>();
            DependsOn<SharedDocumentStore>();
            RegisterStartupTask<OutboxCleaner>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store =
                // Trying pulling a shared DocumentStore set by the user or other Feature
                context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Outbox and no DocumentStore instance found");
            }

            ConnectionVerifier.VerifyConnectionToRavenDBServer(store);
            StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(store, context.Settings);

            Helpers.SafelyCreateIndex(store, new OutboxRecordsIndex());

            context.Container.ConfigureComponent(b => new OutboxPersister(store), DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent<OutboxRecordsCleaner>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(x => x.DocumentStore, store);
        }

        class OutboxCleaner : FeatureStartupTask, IDisposable
        {
            public OutboxRecordsCleaner Cleaner { get; set; }
            public ReadOnlySettings Settings { get; set; }

            public void Dispose()
            {
                if (cleanupTimer != null)
                {
                    cleanupTimer.Dispose();
                    cleanupTimer = null;
                }
            }

            protected override void OnStart()
            {
                timeToKeepDeduplicationData = Settings.GetOrDefault<TimeSpan?>("Outbox.TimeToKeepDeduplicationData") ?? TimeSpan.FromDays(7);

                var frequencyToRunDeduplicationDataCleanup = Settings.GetOrDefault<TimeSpan?>("Outbox.FrequencyToRunDeduplicationDataCleanup") ?? TimeSpan.FromMinutes(1);

                cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(1), frequencyToRunDeduplicationDataCleanup);
            }

            protected override void OnStop()
            {
                using (var waitHandle = new ManualResetEvent(false))
                {
                    cleanupTimer.Dispose(waitHandle);
                    waitHandle.WaitOne();
                    cleanupTimer = null;
                }
            }

            void PerformCleanup(object state)
            {
                Cleaner.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData);
            }

            Timer cleanupTimer;
            TimeSpan timeToKeepDeduplicationData;
        }
    }
}