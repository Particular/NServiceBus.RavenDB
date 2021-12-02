namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;

    /// <summary>
    /// Contains extensions methods for RavenDB-specific outbox configuration.
    /// </summary>
    public static class RavenDBOutboxExtensions
    {
        /// <summary>
        /// Sets the <see cref="TimeSpan" /> to keep deduplication data.
        /// </summary>
        /// <param name="configuration">The <see cref="OutboxSettings" /> being extended.</param>
        /// <param name="timeToKeepDeduplicationData">A positive <see cref="TimeSpan" /> to keep deduplication data.</param>
        /// <remarks>By default, deduplication data is kept for seven days.</remarks>
        public static void SetTimeToKeepDeduplicationData(this OutboxSettings configuration, TimeSpan timeToKeepDeduplicationData) =>
            configuration.GetSettings().SetTimeToKeepDeduplicationData(timeToKeepDeduplicationData);

        static void SetTimeToKeepDeduplicationData(this SettingsHolder settings, TimeSpan timeToKeepDeduplicationData)
        {
            var now = DateTime.UtcNow;
            if (now - timeToKeepDeduplicationData >= now)
            {
                throw new ArgumentOutOfRangeException(nameof(timeToKeepDeduplicationData), "Specify a non-negative TimeSpan. The cleanup process removes entries older than the specified time to keep deduplication data, therefore the time span cannot be negative.");
            }

            settings.Set(RavenDbOutboxStorage.TimeToKeepDeduplicationData, timeToKeepDeduplicationData);
        }

        /// <summary>
        /// Sets the frequency to clean up deduplication data.
        /// </summary>
        /// <param name="configuration">The <see cref="OutboxSettings" /> being extended.</param>
        /// <param name="frequencyToRunDeduplicationDataCleanup">
        /// A positive <see cref="TimeSpan" /> representing the frequency to clean up deduplication data,
        /// or <see cref="System.Threading.Timeout.InfiniteTimeSpan" /> (-1 milliseconds) to disable deduplication data clean up.
        /// </param>
        /// <remarks>By default, deduplication data is cleaned up every 60 seconds.</remarks>
        /// <remarks>
        /// When document expiration is enabled on the database, it is recommended to disable deduplication data clean up
        /// by specifying <see cref="System.Threading.Timeout.InfiniteTimeSpan" /> for
        /// <paramref name="frequencyToRunDeduplicationDataCleanup" />.
        /// </remarks>
        public static void SetFrequencyToRunDeduplicationDataCleanup(this OutboxSettings configuration, TimeSpan frequencyToRunDeduplicationDataCleanup) =>
            configuration.GetSettings().SetFrequencyToRunDeduplicationDataCleanup(frequencyToRunDeduplicationDataCleanup);

        static void SetFrequencyToRunDeduplicationDataCleanup(this SettingsHolder settings, TimeSpan frequencyToRunDeduplicationDataCleanup)
        {
            if (frequencyToRunDeduplicationDataCleanup <= TimeSpan.Zero && frequencyToRunDeduplicationDataCleanup != System.Threading.Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(frequencyToRunDeduplicationDataCleanup), "Provide a non-negative TimeSpan to specify cleanup task execution frequency or specify System.Threading.Timeout.InfiniteTimeSpan to disable cleanup.");
            }

            settings.Set(RavenDbOutboxStorage.FrequencyToRunDeduplicationDataCleanup, frequencyToRunDeduplicationDataCleanup);
        }
    }
}