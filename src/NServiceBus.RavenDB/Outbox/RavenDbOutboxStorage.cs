namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Outbox;

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

            context.Services.AddTransient<IOutboxStorage>(
                sp => new OutboxPersister(context.Settings.EndpointName(), sp.GetRequiredService<IOpenTenantAwareRavenSessions>(), timeToKeepDeduplicationData));

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

            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                if (frequencyToRunDeduplicationDataCleanup == Timeout.InfiniteTimeSpan)
                {
                    return Task.CompletedTask;
                }

                cleanupCancellationTokenSource = new CancellationTokenSource();
                cleanupTask = Task.Run(() => PerformCleanup(cleanupCancellationTokenSource.Token), CancellationToken.None);

                return Task.CompletedTask;
            }

            protected override async Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
            {
                cleanupCancellationTokenSource.Cancel();

                if (cleanupTask == null)
                {
                    return;
                }

                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                var finishedTask = await Task.WhenAny(cleanupTask, timeoutTask).ConfigureAwait(false);

                // This will throw OperationCancelled if invoked because of the cancellationToken
                await finishedTask.ConfigureAwait(false);

                if (finishedTask == timeoutTask)
                {
                    // Was the result of the pre-existing 30s timeout
                    logger.Error("RavenOutboxCleaner failed to stop within the maximum time allowed (30s).");
                }
            }

            async Task PerformCleanup(CancellationToken cancellationToken)
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
                    catch (OperationCanceledException ex)
                    {
                        // Graceful shutdown
                        if (cancellationToken.IsCancellationRequested)
                        {
                            logger.Debug("RavenDB outbox cleanup cancelled.", ex);
                        }
                        else
                        {
                            logger.Warn("OperationCanceledException thrown.", ex);
                        }
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
            CancellationTokenSource cleanupCancellationTokenSource;

            static readonly ILog logger = LogManager.GetLogger<OutboxCleaner>();
        }
    }
}
