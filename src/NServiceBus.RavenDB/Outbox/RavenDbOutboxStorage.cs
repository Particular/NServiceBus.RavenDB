namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations.Expiration;

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

            var frequencyToRunDeduplicationDataCleanup = context.Settings.GetOrDefault<TimeSpan?>(FrequencyToRunDeduplicationDataCleanup) ?? TimeSpan.FromMinutes(1);
            var timeToKeepDeduplicationData = context.Settings.GetOrDefault<TimeSpan?>(TimeToKeepDeduplicationData) ?? TimeSpan.FromDays(7);
            var expirationEnabled = context.Settings.HasSetting(DocumentationExpirationFrequency);
            var expirationFrequency = context.Settings.GetOrDefault<TimeSpan?>(DocumentationExpirationFrequency);

            context.Container.ConfigureComponent(builder =>
                new OutboxPersister(context.Settings.EndpointName(), builder.Build<IOpenTenantAwareRavenSessions>(), timeToKeepDeduplicationData), DependencyLifecycle.InstancePerCall);

            context.RegisterStartupTask(builder =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Outbox>(context.Settings, builder);
                return new Expiration(store, expirationEnabled, expirationFrequency);
            });
            context.RegisterStartupTask(builder =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Outbox>(context.Settings, builder);
                return new OutboxCleaner(new OutboxRecordsCleaner(store), frequencyToRunDeduplicationDataCleanup, timeToKeepDeduplicationData);
            });

            context.Settings.AddStartupDiagnosticsSection(
                "NServiceBus.Persistence.RavenDB.Outbox",
                new
                {
                    ExpirationEnabled = expirationEnabled,
                    ExpirationFrequencySetForDatabase = expirationFrequency,
                    FrequencyToRunDeduplicationDataCleanup = frequencyToRunDeduplicationDataCleanup,
                    TimeToKeepDeduplicationData = timeToKeepDeduplicationData,
                });
        }

        internal const string TimeToKeepDeduplicationData = "Outbox.TimeToKeepDeduplicationData";
        internal const string FrequencyToRunDeduplicationDataCleanup = "Outbox.FrequencyToRunDeduplicationDataCleanup";
        internal const string DocumentationExpirationFrequency = "Outbox.DocumentationExpirationFrequency";

        class Expiration : FeatureStartupTask
        {
            public Expiration(IDocumentStore store, bool expirationEnabled, TimeSpan? expirationFrequency)
            {
                this.expirationEnabled = expirationEnabled;
                this.expirationFrequency = expirationFrequency;
                this.store = store;
            }

            protected override async Task OnStart(IMessageSession session)
            {
                if (!expirationEnabled)
                {
                    return;
                }

                try
                {
                    await store.Maintenance.SendAsync(new ConfigureExpirationOperation(new ExpirationConfiguration
                    {
                        Disabled = false,
                        DeleteFrequencyInSec = Convert.ToInt64(expirationFrequency.Value.TotalSeconds)
                    })).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.Warn(
                        $"Unable to set the expiration frequency to '{expirationFrequency}'. Potentially lacking permissions to execute the required maintenance operation. Make sure to set the expiration frequency on the server in the database directly.", e);
                }
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.CompletedTask;
            }

            IDocumentStore store;
            TimeSpan? expirationFrequency;
            bool expirationEnabled;

            static readonly ILog logger = LogManager.GetLogger<OutboxCleaner>();
        }

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