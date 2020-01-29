namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Settings;

    class RavenDbOutboxStorage : Feature
    {
        public RavenDbOutboxStorage()
        {
            DependsOn<Outbox>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var endpointName = context.Settings.EndpointName();

            DocumentStoreManager.GetUninitializedDocumentStore<StorageType.Outbox>(context.Settings)
                .SafelyCreateIndex(new OutboxRecordsIndex());

            context.Container.ConfigureComponent(b =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Outbox>(context.Settings, b);
                return new OutboxPersister(store, endpointName, b.Build<IOpenRavenSessionsInPipeline>());
            }, DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Outbox>(context.Settings, b);
                return new OutboxRecordsCleaner(store);
            }, DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b => new OutboxCleaner(b.Build<OutboxRecordsCleaner>(), context.Settings), DependencyLifecycle.InstancePerCall);

            context.RegisterStartupTask(builder => builder.Build<OutboxCleaner>());
        }

        class OutboxCleaner : FeatureStartupTask
        {
            public OutboxCleaner(OutboxRecordsCleaner cleaner, ReadOnlySettings settings)
            {
                this.cleaner = cleaner;
                this.settings = settings;
                logger = LogManager.GetLogger<OutboxCleaner>();
            }

            protected override Task OnStart(IMessageSession session)
            {
                frequencyToRunDeduplicationDataCleanup = settings.GetOrDefault<TimeSpan?>("Outbox.FrequencyToRunDeduplicationDataCleanup") ?? TimeSpan.FromMinutes(1);

                if (frequencyToRunDeduplicationDataCleanup == Timeout.InfiniteTimeSpan)
                {
                    return Task.CompletedTask;
                }

                timeToKeepDeduplicationData = settings.GetOrDefault<TimeSpan?>("Outbox.TimeToKeepDeduplicationData") ?? TimeSpan.FromDays(7);

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

            Task cleanupTask;
            OutboxRecordsCleaner cleaner;
            ReadOnlySettings settings;
            TimeSpan timeToKeepDeduplicationData;
            TimeSpan frequencyToRunDeduplicationDataCleanup;
            CancellationTokenSource cancellationTokenSource;
            CancellationToken cancellationToken;
            ILog logger;
        }
    }
}