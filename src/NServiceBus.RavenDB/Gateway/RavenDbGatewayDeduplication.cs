namespace NServiceBus.Features
{
    using NServiceBus.Persistence;
    using NServiceBus.RavenDB.Gateway.Deduplication;
    using NServiceBus.RavenDB.Internal;

    class RavenDbGatewayDeduplication : Feature
    {
        RavenDbGatewayDeduplication()
        {
            DependsOn("Gateway");
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store = DocumentStoreManager.GetDocumentStore<StorageType.GatewayDeduplication>(context.Settings);

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(c => c.StoreToInstall, store);

            context.Container.ConfigureComponent<RavenDeduplication>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.DocumentStore, store);
        }
    }
}