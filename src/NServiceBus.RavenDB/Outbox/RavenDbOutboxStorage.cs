namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using System.Configuration;
    using System.Threading;
    using NServiceBus.Features;
    using NServiceBus.Outbox.RavenDB;
    using NServiceBus.RavenDB.Internal;
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

            Helpers.SafelyCreateIndex(store, new OutboxRecordsIndex());

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(c => c.StoreToInstall, store);

            context.Container.ConfigureComponent<OutboxPersister>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(x => x.DocumentStore, store);

            context.Container.ConfigureComponent<OutboxRecordsCleaner>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(x => x.DocumentStore, store);
        }

        class OutboxCleaner : FeatureStartupTask, IDisposable
        {
            Timer cleanupTimer;
            TimeSpan frequencyToRunDeduplicationDataCleanup;
            TimeSpan timeToKeepDeduplicationData;
            public OutboxRecordsCleaner Cleaner { get; set; }

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
                var configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox/RavenDB/TimeToKeepDeduplicationData");

                if (configValue == null)
                {
                    timeToKeepDeduplicationData = TimeSpan.FromDays(7);
                }
                else
                {
                    if (TimeSpan.TryParse(configValue, out timeToKeepDeduplicationData))
                    {
                        throw new Exception("Invalid value in \"NServiceBus/Outbox/RavenDB/TimeToKeepDeduplicationData\" AppSetting. Please ensure it is a TimeSpan.");
                    }
                }

                configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox/RavenDB/FrequencyToRunDeduplicationDataCleanup");

                if (configValue == null)
                {
                    frequencyToRunDeduplicationDataCleanup = TimeSpan.FromMinutes(1);
                }
                else
                {
                    if (TimeSpan.TryParse(configValue, out frequencyToRunDeduplicationDataCleanup))
                    {
                        throw new Exception("Invalid value in \"NServiceBus/Outbox/RavenDB/FrequencyToRunDeduplicationDataCleanup\" AppSetting. Please ensure it is a TimeSpan.");
                    }
                }

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
        }
    }
}