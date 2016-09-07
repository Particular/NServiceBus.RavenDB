namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Persistence;

    class RavenDbOutboxStorage : Feature
    {
        public RavenDbOutboxStorage()
        {
            DependsOn<Outbox>();
            DependsOn<SharedDocumentStore>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store = DocumentStoreManager.GetDocumentStore<StorageType.Outbox>(context.Settings);

            Helpers.SafelyCreateIndex(store, new OutboxRecordsIndex());

            context.Container.ConfigureComponent(b => new OutboxPersister(store, context.Settings.EndpointName()), DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b => new OutboxRecordsCleaner(store), DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent<OutboxCleaner>(DependencyLifecycle.InstancePerCall);

            var timeToKeepDeduplicationData = context.Settings.GetOrDefault<TimeSpan?>("Outbox.TimeToKeepDeduplicationData") ?? TimeSpan.FromDays(7);

            var frequencyToRunDeduplicationDataCleanup = context.Settings.GetOrDefault<TimeSpan?>("Outbox.FrequencyToRunDeduplicationDataCleanup") ?? TimeSpan.FromMinutes(1);

            context.RegisterStartupTask(builder => new OutboxCleaner(builder.Build<OutboxRecordsCleaner>(), timeToKeepDeduplicationData, frequencyToRunDeduplicationDataCleanup));
        }

        class OutboxCleaner : FeatureStartupTask
        {
            public OutboxCleaner(OutboxRecordsCleaner cleaner, TimeSpan timeToKeepDeduplicationData, TimeSpan frequencyToRunDeduplicationDataCleanup)
            {
                this.cleaner = cleaner;
                this.logger = LogManager.GetLogger<OutboxCleaner>();
                this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
                this.frequencyToRunDeduplicationDataCleanup = frequencyToRunDeduplicationDataCleanup;
            }

            protected override Task OnStart(IMessageSession session)
            {
                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;

                cleanupTask = Task.Run(() => PerformCleanup(), CancellationToken.None);

                return TaskEx.CompletedTask;
            }

            protected override async Task OnStop(IMessageSession session)
            {
                cancellationTokenSource.Cancel();

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

            Task cleanupTask;
            OutboxRecordsCleaner cleaner;
            TimeSpan timeToKeepDeduplicationData;
            TimeSpan frequencyToRunDeduplicationDataCleanup;
            CancellationTokenSource cancellationTokenSource;
            CancellationToken cancellationToken;
            ILog logger;
        }
    }
}