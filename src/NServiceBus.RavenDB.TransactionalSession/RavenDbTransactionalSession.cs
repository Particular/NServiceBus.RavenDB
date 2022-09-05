namespace NServiceBus.TransactionalSession
{
    using Features;

    sealed class RavenDbTransactionalSession : Feature
    {
        public RavenDbTransactionalSession()
        {
            DependsOn<SynchronizedStorage>();
            DependsOn<TransactionalSession>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}