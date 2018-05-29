namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;

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
        public static BusConfiguration SetTimeToKeepDeduplicationData(this BusConfiguration configuration, TimeSpan timeToKeepDeduplicationData)
        {
            var now = DateTime.UtcNow;
            if (now - timeToKeepDeduplicationData >= now)
            {
                throw new ArgumentException("Please Specify a non-negative TimeSpan. The cleanup process removes entries older than the specified time to keep deduplication data, therefore the time span cannot be negative.", "timeToKeepDeduplicationData");
            }

            configuration.GetSettings().Set("Outbox.TimeToKeepDeduplicationData", timeToKeepDeduplicationData);
            return configuration;
        }

        /// <summary>
        /// Sets the frequency to run the deduplication data cleanup task.
        /// </summary>
        /// <param name="configuration">The configuration being extended</param>
        /// <param name="frequencyToRunDeduplicationDataCleanup">The frequency to run the deduplication data cleanup task. By specifying a negative time span (-1) the cleanup task will never run.</param>
        /// <returns>The configuration</returns>
        public static BusConfiguration SetFrequencyToRunDeduplicationDataCleanup(this BusConfiguration configuration, TimeSpan frequencyToRunDeduplicationDataCleanup)
        {
            configuration.GetSettings().Set("Outbox.FrequencyToRunDeduplicationDataCleanup", frequencyToRunDeduplicationDataCleanup);
            return configuration;
        }

        /// <summary>
        /// Disables the built-in outbox cleanup process.
        /// </summary>
        /// <returns>The configuration</returns>
        public static BusConfiguration DisableOutboxCleanup(this BusConfiguration configuration)
        {
            configuration.GetSettings().Set(RavenDbOutboxStorage.DisableCleanupSettingKey, true);
            return configuration;
        }
    }
}