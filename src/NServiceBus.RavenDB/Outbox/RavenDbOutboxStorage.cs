namespace NServiceBus.RavenDB.Outbox
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

            context.Container.ConfigureComponent(b => new OutboxRecordsCleaner(store), DependencyLifecycle.InstancePerCall);
              
            context.Container.ConfigureComponent<OutboxCleaner>(DependencyLifecycle.InstancePerCall);

            context.RegisterStartupTask(builder => builder.Build<OutboxCleaner>());
        }

        class OutboxCleaner : FeatureStartupTask, IDisposable
        {
            public OutboxCleaner(OutboxRecordsCleaner cleaner, ReadOnlySettings settings)
            {
                this.cleaner = cleaner;
                this.settings = settings;
            }


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
                timeToKeepDeduplicationData = settings.GetOrDefault<TimeSpan?>("Outbox.TimeToKeepDeduplicationData") ?? TimeSpan.FromDays(7);

                var frequencyToRunDeduplicationDataCleanup = settings.GetOrDefault<TimeSpan?>("Outbox.FrequencyToRunDeduplicationDataCleanup") ?? TimeSpan.FromMinutes(1);

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
                cleaner.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData)
                    .GetAwaiter()
                    .GetResult();
            }


            OutboxRecordsCleaner cleaner;
            ReadOnlySettings settings;
            Timer cleanupTimer;
            TimeSpan timeToKeepDeduplicationData;
        }
    }
}