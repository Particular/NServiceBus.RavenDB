namespace NServiceBus.RavenDB.Shutdown
{
    /// <summary>
    /// This class is to be ported to the Endpoint level in NServiceBus core.
    /// This allows us to fix the timeout shutdown bug without also needing to patch core.
    /// </summary>
    class ShutdownHook : IWantToRunWhenBusStartsAndStops
    {
        public ShutdownHook(IContainShutdownDelegates shutdownRegistry)
        {
            this.shutdownRegistry = shutdownRegistry;
        }

        public void Start()
        {
        }

        public void Stop()
        {
            var shutdownDelegates = shutdownRegistry.GetDelegates();
            foreach (var shutdownDelegate in shutdownDelegates)
            {
                shutdownDelegate.Invoke();
            }
        }

        IContainShutdownDelegates shutdownRegistry;
    }
}
