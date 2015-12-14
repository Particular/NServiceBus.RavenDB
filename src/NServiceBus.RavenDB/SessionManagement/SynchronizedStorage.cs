using NServiceBus.Features;
using NServiceBus.RavenDB.Internal;

namespace NServiceBus.RavenDB.SessionManagement
{
    class SynchronizedStorage : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<RavenDBSynchronizedStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}
