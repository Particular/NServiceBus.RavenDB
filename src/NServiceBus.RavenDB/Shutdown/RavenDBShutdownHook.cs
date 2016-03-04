namespace NServiceBus.Features
{
    using NServiceBus.RavenDB.Shutdown;

    class RavenDbShutdownHook : Feature
    {
        RavenDbShutdownHook()
        {
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<ShutdownDelegateRegistry>(DependencyLifecycle.SingleInstance);
        }
    }
}
