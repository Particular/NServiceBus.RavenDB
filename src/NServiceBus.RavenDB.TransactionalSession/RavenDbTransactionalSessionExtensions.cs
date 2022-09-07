namespace NServiceBus.TransactionalSession
{
    using Configuration.AdvancedExtensibility;
    using Features;

    /// <summary>
    /// Enables the transactional session feature.
    /// </summary>
    public static class RavenDbTransactionalSessionExtensions
    {
        /// <summary>
        /// Enables transactional session for this endpoint.
        /// </summary>
        public static PersistenceExtensions<RavenDBPersistence> EnableTransactionalSession(
            this PersistenceExtensions<RavenDBPersistence> persistenceExtensions)
        {
            persistenceExtensions.GetSettings().EnableFeatureByDefault<RavenDbTransactionalSession>();
            return persistenceExtensions;
        }
    }
}