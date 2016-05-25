namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.Features;
    using NServiceBus.Persistence;

    class RavenDbGatewayDeduplication : Feature
    {
        RavenDbGatewayDeduplication()
        {
            DependsOn("NServiceBus.Features.Gateway");
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store = DocumentStoreManager.GetDocumentStore<StorageType.GatewayDeduplication>(context.Settings);

            context.Container.ConfigureComponent(b=>new RavenDeduplication(store), DependencyLifecycle.SingleInstance);
        }
    }
}