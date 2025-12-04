namespace NServiceBus.Persistence.RavenDB;

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
        Enable<RavenDbStorageSession>();

        DependsOn<Outbox>();
        DependsOn<RavenDbStorageSession>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        DocumentStoreManager.GetUninitializedDocumentStore<StorageType.Outbox>(context.Settings)
            .CreateIndexOnInitialization(new OutboxRecordsIndex());

        var timeToKeepDeduplicationData = context.Settings.GetOrDefault<TimeSpan?>(TimeToKeepDeduplicationData) ?? DeduplicationDataTTLDefault;
        var useClusterWideTransactions = context.Settings.GetOrDefault<bool>(RavenDbStorageSession.UseClusterWideTransactions);
        var endpointName = context.Settings.GetOrDefault<string>(ProcessorEndpointKey) ?? context.Settings.EndpointName();

        context.Services.AddTransient<IOutboxStorage>(
            sp => new OutboxPersister(endpointName, sp.GetRequiredService<IOpenTenantAwareRavenSessions>(), timeToKeepDeduplicationData, useClusterWideTransactions));

        var frequencyToRunDeduplicationDataCleanup = context.Settings.GetOrDefault<TimeSpan?>(FrequencyToRunDeduplicationDataCleanup) ?? Timeout.InfiniteTimeSpan;

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
                ClusterWideTransactions = useClusterWideTransactions ? "Enabled" : "Disabled",
            });
    }

    internal const string TimeToKeepDeduplicationData = "Outbox.TimeToKeepDeduplicationData";
    internal const string ProcessorEndpointKey = "RavenDB.TransactionalSession.ProcessorEndpoint";
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

            // Task.Run() so the call returns immediately instead of waiting for the first await or return down the call stack
            cleanupTask = Task.Run(() => PerformCleanupAndSwallowExceptions(cleanupCancellationTokenSource.Token), CancellationToken.None);

            return Task.CompletedTask;
        }

        protected override async Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
        {
            if (frequencyToRunDeduplicationDataCleanup == Timeout.InfiniteTimeSpan)
            {
                return;
            }

            cleanupCancellationTokenSource.Cancel();

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            var finishedTask = await Task.WhenAny(cleanupTask, timeoutTask).ConfigureAwait(false);

            // This will throw OperationCanceledException if invoked because of the cancellationToken
            await finishedTask.ConfigureAwait(false);

            if (finishedTask == timeoutTask)
            {
                // Was the result of the pre-existing 30s timeout
                Logger.Error("RavenOutboxCleaner failed to stop within the maximum time allowed (30s).");
            }
        }

        async Task PerformCleanupAndSwallowExceptions(CancellationToken cancellationToken)
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
                catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
                {
                    // private token, cleaner is being stopped, log exception in case the stack trace is ever needed for debugging
                    Logger.Debug("Operation canceled while stopping outbox cleaner.", ex);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error("Unable to remove expired Outbox records from Raven database.", ex);
                }
            }
        }

        readonly OutboxRecordsCleaner cleaner;
        Task cleanupTask;
        TimeSpan timeToKeepDeduplicationData;
        TimeSpan frequencyToRunDeduplicationDataCleanup;
        CancellationTokenSource cleanupCancellationTokenSource;

        static readonly ILog Logger = LogManager.GetLogger<OutboxCleaner>();
    }
}