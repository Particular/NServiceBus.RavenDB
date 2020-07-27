namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Logging;

    class RavenDbOutboxStorage : Feature
    {
        public RavenDbOutboxStorage()
        {
            Defaults(s => s.EnableFeatureByDefault<RavenDbStorageSession>());
            DependsOn<Outbox>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            DocumentStoreManager.GetUninitializedDocumentStore<StorageType.Outbox>(context.Settings)
                .CreateIndexOnInitialization(new OutboxRecordsIndex());

            var timeToKeepDeduplicationData = context.Settings.GetOrDefault<TimeSpan?>(TimeToKeepDeduplicationData) ?? DeduplicationDataTTLDefault;

            context.Container.ConfigureComponent(
                builder => new OutboxPersister(context.Settings.EndpointName(), builder.Build<IOpenTenantAwareRavenSessions>(), timeToKeepDeduplicationData),
                DependencyLifecycle.InstancePerCall);

            var frequencyToRunDeduplicationDataCleanup = context.Settings.GetOrDefault<TimeSpan?>(FrequencyToRunDeduplicationDataCleanup) ?? TimeSpan.FromMinutes(1);

            context.RegisterStartupTask(builder =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Outbox>(context.Settings, builder);
                return new OutboxCleaner(new OutboxRecordsCleaner(store), frequencyToRunDeduplicationDataCleanup, timeToKeepDeduplicationData);
            });

            context.Settings.AddStartupDiagnosticsSection(
                "NServiceBus.Persistence.RavenDB.Outbox",
                new
                {
                    FrequencyToRunDeduplicationDataCleanup = frequencyToRunDeduplicationDataCleanup,
                    TimeToKeepDeduplicationData = timeToKeepDeduplicationData,
                });
        }

        internal const string TimeToKeepDeduplicationData = "Outbox.TimeToKeepDeduplicationData";
        internal const string FrequencyToRunDeduplicationDataCleanup = "Outbox.FrequencyToRunDeduplicationDataCleanup";
        internal static readonly TimeSpan DeduplicationDataTTLDefault = TimeSpan.FromDays(7);

        class OutboxCleaner : FeatureStartupTask
        {
            public OutboxCleaner(OutboxRecordsCleaner cleaner, TimeSpan frequencyToRunDeduplicationDataCleanup, TimeSpan timeToKeepDeduplicationData)
            {
                this.cleaner = cleaner;
                this.frequencyToRunDeduplicationDataCleanup = frequencyToRunDeduplicationDataCleanup;
                this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
            }

            protected override Task OnStart(IMessageSession session)
            {
                if (frequencyToRunDeduplicationDataCleanup == Timeout.InfiniteTimeSpan)
                {
                    return Task.CompletedTask;
                }

                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;

                cleanupTask = Task.Run(() => PerformCleanup(), CancellationToken.None);

                return Task.CompletedTask;
            }

            protected override async Task OnStop(IMessageSession session)
            {
                cancellationTokenSource.Cancel();

                if (cleanupTask == null)
                {
                    return;
                }

                // ReSharper disable once MethodSupportsCancellation
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                var finishedTask = await Task.WhenAny(cleanupTask, timeoutTask).ConfigureAwait(false);

                if (finishedTask == timeoutTask)
                {
                    logger.Error("RavenOutboxCleaner failed to stop within the time allowed (30s).");
                }
            }

            async Task PerformCleanup()
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var nextClean = DateTime.UtcNow.Add(frequencyToRunDeduplicationDataCleanup);

                        var olderThan = DateTime.UtcNow - timeToKeepDeduplicationData;
                        await cleaner.RemoveEntriesOlderThan(olderThan, cancellationToken).ConfigureAwait(false);

                        var delay = nextClean - DateTime.UtcNow;
                        if (delay > TimeSpan.Zero)
                        {
                            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Graceful shutdown
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Unable to remove expired Outbox records from Raven database.", ex);
                    }
                }
            }

            readonly OutboxRecordsCleaner cleaner;
            Task cleanupTask;
            TimeSpan timeToKeepDeduplicationData;
            TimeSpan frequencyToRunDeduplicationDataCleanup;
            CancellationTokenSource cancellationTokenSource;
            CancellationToken cancellationToken;

            static readonly ILog logger = LogManager.GetLogger<OutboxCleaner>();
        }
    }
}