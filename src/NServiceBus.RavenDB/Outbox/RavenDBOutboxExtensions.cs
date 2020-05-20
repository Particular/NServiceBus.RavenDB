namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence.RavenDB;

    /// <summary>
    /// Contains extensions methods which allow to configure RavenDB outbox specific configuration
    /// </summary>
    public static class RavenDBOutboxExtensions
    {
        /// <summary>
        /// Sets the time to keep the deduplication data to the specified time span.
        /// </summary>
        /// <param name="configuration">The configuration being extended</param>
        /// <param name="timeToKeepDeduplicationData">The time to keep the deduplication data.
        /// The cleanup process removes entries older than the specified time to keep deduplication data, therefore the time span cannot be negative</param>
        /// <returns>The configuration</returns>
        public static void SetTimeToKeepDeduplicationData(this OutboxSettings configuration, TimeSpan timeToKeepDeduplicationData)
        {
            var now = DateTime.UtcNow;
            if (now - timeToKeepDeduplicationData >= now)
            {
                throw new ArgumentOutOfRangeException(nameof(timeToKeepDeduplicationData), "Specify a non-negative TimeSpan. The cleanup process removes entries older than the specified time to keep deduplication data, therefore the time span cannot be negative.");
            }

            configuration.GetSettings().Set(RavenDbOutboxStorage.TimeToKeepDeduplicationData, timeToKeepDeduplicationData);
        }

        /// <summary>
        /// Sets the frequency to run the deduplication data cleanup task.
        /// </summary>
        /// <param name="configuration">The configuration being extended</param>
        /// <param name="frequencyToRunDeduplicationDataCleanup">The frequency to run the deduplication data cleanup task. By specifying <code>System.Threading.Timeout.InfiniteTimeSpan</code> (-1 milliseconds) the cleanup task will never run. The default cleanup interval is 60 seconds.</param>
        /// <returns>The configuration</returns>
        public static void SetFrequencyToRunDeduplicationDataCleanup(this OutboxSettings configuration, TimeSpan frequencyToRunDeduplicationDataCleanup)
        {
            if (frequencyToRunDeduplicationDataCleanup <= TimeSpan.Zero && frequencyToRunDeduplicationDataCleanup != System.Threading.Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(frequencyToRunDeduplicationDataCleanup), "Provide a non-negative TimeSpan to specify cleanup task execution frequency or specify System.Threading.Timeout.InfiniteTimeSpan to disable cleanup.");
            }

            configuration.GetSettings().Set(RavenDbOutboxStorage.FrequencyToRunDeduplicationDataCleanup, frequencyToRunDeduplicationDataCleanup);
        }

        /// <summary>
        /// Enables document expiration on the database managed by the document store.
        /// </summary>
        /// <param name="configuration">The configuration being extended</param>
        /// <param name="frequencyToRunExpiry">The frequency to run the data expiry in the database on the server. The default is every 60 seconds.</param>
        /// <returns>The configuration</returns>
        public static void EnableDocumentExpiration(this OutboxSettings configuration, TimeSpan? frequencyToRunExpiry = null)
        {
            configuration.GetSettings().Set(RavenDbOutboxStorage.DocumentationExpirationFrequency, frequencyToRunExpiry ?? TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Sets the time to keep the deduplication data to the specified time span.
        /// </summary>
        /// <param name="configuration">The configuration being extended</param>
        /// <param name="timeToKeepDeduplicationData">The time to keep the deduplication data.
        /// The cleanup process removes entries older than the specified time to keep deduplication data, therefore the time span cannot be negative</param>
        /// <returns>The configuration</returns>
        [ObsoleteEx(
            Message = "Use `SetTimeToKeepDeduplicationData` available on the `OutboxSettings` instead.",
            TreatAsErrorFromVersion = "6.0.0",
            RemoveInVersion = "7.0.0")]
        public static EndpointConfiguration SetTimeToKeepDeduplicationData(this EndpointConfiguration configuration, TimeSpan timeToKeepDeduplicationData)
        {
            throw new NotImplementedException("Use `SetTimeToKeepDeduplicationData` available on the `OutboxSettings` instead.");
        }

        /// <summary>
        /// Sets the frequency to run the deduplication data cleanup task.
        /// </summary>
        /// <param name="configuration">The configuration being extended</param>
        /// <param name="frequencyToRunDeduplicationDataCleanup">The frequency to run the deduplication data cleanup task. By specifying <code>System.Threading.Timeout.InfiniteTimeSpan</code> (-1 milliseconds) the cleanup task will never run. The default cleanup interval is 60 seconds.</param>
        /// <returns>The configuration</returns>
        [ObsoleteEx(
            Message = "Use `SetFrequencyToRunDeduplicationDataCleanup` available on the `OutboxSettings` instead.",
            TreatAsErrorFromVersion = "6.0.0",
            RemoveInVersion = "7.0.0")]
        public static EndpointConfiguration SetFrequencyToRunDeduplicationDataCleanup(this EndpointConfiguration configuration, TimeSpan frequencyToRunDeduplicationDataCleanup)
        {
            throw new NotImplementedException("Use `SetFrequencyToRunDeduplicationDataCleanup` available on the `OutboxSettings` instead.");
        }
    }
}
