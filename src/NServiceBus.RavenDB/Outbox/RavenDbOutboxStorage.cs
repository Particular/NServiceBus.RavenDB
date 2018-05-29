namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using System.Threading;
    using NServiceBus.Features;
    using NServiceBus.Outbox.RavenDB;
    using NServiceBus.Persistence;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.Settings;

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
            var store = DocumentStoreManager.GetDocumentStore<StorageType.Outbox>(context.Settings);

            Helpers.SafelyCreateIndex(store, new OutboxRecordsIndex());

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(c => c.StoreToInstall, store);

            context.Container.ConfigureComponent<OutboxPersister>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(x => x.DocumentStore, store)
                .ConfigureProperty(x => x.EndpointName, context.Settings.EndpointName());

            context.Container.ConfigureComponent(b => new OutboxRecordsCleaner(store), DependencyLifecycle.InstancePerCall);
        }

        class OutboxCleaner : FeatureStartupTask, IDisposable
        {
            Timer cleanupTimer;
            TimeSpan timeToKeepDeduplicationData;
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
                if (Settings.GetOrDefault<bool>(DisableCleanupSettingKey))
                {
                    return;
                }

                timeToKeepDeduplicationData = Settings.GetOrDefault<TimeSpan?>("Outbox.TimeToKeepDeduplicationData") ?? TimeSpan.FromDays(7);

                var frequencyToRunDeduplicationDataCleanup = Settings.GetOrDefault<TimeSpan?>("Outbox.FrequencyToRunDeduplicationDataCleanup") ?? TimeSpan.FromMinutes(1);

                cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(1), frequencyToRunDeduplicationDataCleanup);
            }

            protected override void OnStop()
            {
                if (Settings.GetOrDefault<bool>(DisableCleanupSettingKey))
                {
                    return;
                }

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
        }

        internal static string DisableCleanupSettingKey = "Outbox.DisableCleanup";
    }
}