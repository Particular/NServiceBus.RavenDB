namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.Features;

    class SharedDocumentStore : Feature
    {
        public SharedDocumentStore()
        {
            Defaults(_ => _.Set<SingleSharedDocumentStore>(new SingleSharedDocumentStore()));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // no-op, the docstore is initialized lazily
        }
    }
}