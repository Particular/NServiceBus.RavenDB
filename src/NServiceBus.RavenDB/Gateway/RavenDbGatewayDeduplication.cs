namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.Features;

    class RavenDbGatewayDeduplication : Feature
    {
        RavenDbGatewayDeduplication()
        {
            DependsOn("NServiceBus.Features.Gateway");
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(builder =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.GatewayDeduplication>(context.Settings, builder);
                return new RavenDeduplication(store);
            }, DependencyLifecycle.SingleInstance);
        }
    }
}