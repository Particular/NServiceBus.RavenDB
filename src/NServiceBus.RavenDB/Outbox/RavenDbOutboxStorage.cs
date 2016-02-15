﻿namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
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
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store =
                // Trying pulling a shared DocumentStore set by the user or other Feature
                context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if(store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Outbox and no DocumentStore instance found");
            }

            StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(store, context.Settings);

            Helpers.SafelyCreateIndex(store, new OutboxRecordsIndex());

            context.Container.ConfigureComponent(b => new OutboxPersister(store, context.Settings.EndpointName()), DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent<OutboxRecordsCleaner>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(x => x.DocumentStore, store);

            context.Container.ConfigureComponent<OutboxCleaner>(DependencyLifecycle.InstancePerCall);

            context.RegisterStartupTask(builder =>
            {
                return builder.Build<OutboxCleaner>();
            });
        }

        class OutboxCleaner : FeatureStartupTask, IDisposable
        {
            public OutboxRecordsCleaner Cleaner { get; set; }
            public ReadOnlySettings Settings { get; set; }

            public void Dispose()
            {
                if(cleanupTimer != null)
                {
                    cleanupTimer.Dispose();
                    cleanupTimer = null;
                }
            }

            protected override Task OnStart(IMessageSession session)
            {
                timeToKeepDeduplicationData = Settings.GetOrDefault<TimeSpan?>("Outbox.TimeToKeepDeduplicationData") ?? TimeSpan.FromDays(7);

                var frequencyToRunDeduplicationDataCleanup = Settings.GetOrDefault<TimeSpan?>("Outbox.FrequencyToRunDeduplicationDataCleanup") ?? TimeSpan.FromMinutes(1);

                cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(1), frequencyToRunDeduplicationDataCleanup);

                return Task.FromResult(0);
            }

            protected override Task OnStop(IMessageSession session)
            {
                using(var waitHandle = new ManualResetEvent(false))
                {
                    cleanupTimer.Dispose(waitHandle);
                    waitHandle.WaitOne();
                    cleanupTimer = null;
                }

                return Task.FromResult(0);
            }

            void PerformCleanup(object state)
            {
                Cleaner.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData)
                    .GetAwaiter()
                    .GetResult();
            }

            Timer cleanupTimer;
            TimeSpan timeToKeepDeduplicationData;
        }
    }
}