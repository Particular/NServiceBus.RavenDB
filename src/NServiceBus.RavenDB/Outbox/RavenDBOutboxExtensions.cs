namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvancedExtensibility;
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
        public static EndpointConfiguration SetTimeToKeepDeduplicationData(this EndpointConfiguration configuration, TimeSpan timeToKeepDeduplicationData)
        {
            var now = DateTime.UtcNow;
            if (now - timeToKeepDeduplicationData >= now)
            {
                throw new ArgumentOutOfRangeException(nameof(timeToKeepDeduplicationData), "Specify a non-negative TimeSpan. The cleanup process removes entries older than the specified time to keep deduplication data, therefore the time span cannot be negative.");
            }

            configuration.GetSettings().Set(RavenDbOutboxStorage.TimeToKeepDeduplicationData, timeToKeepDeduplicationData);
            return configuration;
        }

        /// <summary>
        /// Sets the frequency to run the deduplication data cleanup task.
        /// </summary>
        /// <param name="configuration">The configuration being extended</param>
        /// <param name="frequencyToRunDeduplicationDataCleanup">The frequency to run the deduplication data cleanup task. By specifying <code>System.Threading.Timeout.InfiniteTimeSpan</code> (-1 milliseconds) the cleanup task will never run.</param>
        /// <returns>The configuration</returns>
        /// <remarks>When document expiration is enabled on the database, it is recommended to disable the cleanup task by specifying <code>System.Threading.Timeout.InfiniteTimeSpan</code> (-1 milliseconds) for <paramref name="frequencyToRunDeduplicationDataCleanup"/>.</remarks>
        public static EndpointConfiguration SetFrequencyToRunDeduplicationDataCleanup(this EndpointConfiguration configuration, TimeSpan frequencyToRunDeduplicationDataCleanup)
        {
            if (frequencyToRunDeduplicationDataCleanup <= TimeSpan.Zero && frequencyToRunDeduplicationDataCleanup != System.Threading.Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(frequencyToRunDeduplicationDataCleanup), "Provide a non-negative TimeSpan to specify cleanup task execution frequency or specify System.Threading.Timeout.InfiniteTimeSpan to disable cleanup.");
            }

            configuration.GetSettings().Set(RavenDbOutboxStorage.FrequencyToRunDeduplicationDataCleanup, frequencyToRunDeduplicationDataCleanup);
            return configuration;
        }
    }
}
