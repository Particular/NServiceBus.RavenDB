namespace NServiceBus.TransactionalSession
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Features;
    using Persistence.RavenDB;

    /// <summary>
    /// Enables the transactional session feature.
    /// </summary>
    public static class RavenDbTransactionalSessionExtensions
    {
        /// <summary>
        /// Enables transactional session for this endpoint.
        /// </summary>
        public static PersistenceExtensions<RavenDBPersistence> EnableTransactionalSession(
            this PersistenceExtensions<RavenDBPersistence> persistenceExtensions) =>
            EnableTransactionalSession(persistenceExtensions, new TransactionalSessionOptions());

        /// <summary>
        /// Enables the transactional session for this endpoint using the specified TransactionalSessionOptions.
        /// </summary>
        public static PersistenceExtensions<RavenDBPersistence> EnableTransactionalSession(this PersistenceExtensions<RavenDBPersistence> persistenceExtensions,
            TransactionalSessionOptions transactionalSessionOptions)
        {
            ArgumentNullException.ThrowIfNull(persistenceExtensions);
            ArgumentNullException.ThrowIfNull(transactionalSessionOptions);

            var settings = persistenceExtensions.GetSettings();

            settings.Set(transactionalSessionOptions);

            if (!string.IsNullOrWhiteSpace(transactionalSessionOptions.ProcessorEndpoint))
            {
                settings.Set(RavenDbOutboxStorage.ProcessorEndpointKey, transactionalSessionOptions.ProcessorEndpoint);
            }

            settings.EnableFeatureByDefault<RavenDbTransactionalSession>();

            return persistenceExtensions;
        }
    }
}