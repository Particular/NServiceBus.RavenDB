namespace NServiceBus.RavenDB.Shutdown
{
    using NServiceBus.TimeoutPersisters.RavenDB;

    /// <summary>
    /// This is a hack to get a hook into shutdown without needing to modify NServiceBus Core V5.
    /// Normally we would prefer to not use a customer abstraction for internal infrastructure.
    /// In CoreV6 this will be achievable via FeatureStartupTask.
    /// </summary>
    class ShutdownTimeoutPersister : IWantToRunWhenBusStartsAndStops
    {
        // Will be injected IF the RavenDbTimeoutStorage feature is activated.
        // Otherwise it will be null and there's nothing to shut down.
        public TimeoutPersister Persister { get; set; }

        public void Start()
        {
        }

        public void Stop()
        {
            if (Persister != null)
            {
                Persister.Shutdown();
            }
        }
    }
}
