namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Outbox;

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
        public static OutboxSettings SetTimeToKeepDeduplicationData(this OutboxSettings configuration, TimeSpan timeToKeepDeduplicationData)
        {
            var now = DateTime.UtcNow;
            if (now - timeToKeepDeduplicationData >= now)
            {
                throw new ArgumentException("Specify a non-negative TimeSpan. The cleanup process removes entries older than the specified time to keep deduplication data, therefore the time span cannot be negative.", "timeToKeepDeduplicationData");
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
        public static OutboxSettings SetFrequencyToRunDeduplicationDataCleanup(this OutboxSettings configuration, TimeSpan frequencyToRunDeduplicationDataCleanup)
        {
            configuration.GetSettings().Set("Outbox.FrequencyToRunDeduplicationDataCleanup", frequencyToRunDeduplicationDataCleanup);
            return configuration;
        }


#pragma warning disable 1591

        [ObsoleteEx(
            RemoveInVersion = "5",
            TreatAsErrorFromVersion = "4",
            Message = "Use endpointConfiguration.EnableOutbox().SetTimeToKeepDeduplicationData(timeToKeepDeduplicationData)")]
        public static EndpointConfiguration SetTimeToKeepDeduplicationData(this EndpointConfiguration configuration, TimeSpan timeToKeepDeduplicationData)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "5",
            TreatAsErrorFromVersion = "4",
            Message = "Use endpointConfiguration.EnableOutbox().SetFrequencyToRunDeduplicationDataCleanup(frequencyToRunDeduplicationDataCleanup)")]
        public static EndpointConfiguration SetFrequencyToRunDeduplicationDataCleanup(this EndpointConfiguration configuration, TimeSpan frequencyToRunDeduplicationDataCleanup)
        {
            throw new NotImplementedException();
        }

#pragma warning restore 1591
    }
}