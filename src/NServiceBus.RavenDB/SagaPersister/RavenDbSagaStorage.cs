namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Linq;
    using NServiceBus.Features;
    using NServiceBus.Sagas;


    class RavenDbSagaStorage : Feature
    {
        internal RavenDbSagaStorage()
        {
            Defaults(s => s.EnableFeatureByDefault<RavenDbStorageSession>());
            DependsOn<Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var options = context.Settings.GetOrDefault<SagaPersistenceConfiguration>() ?? new SagaPersistenceConfiguration();
            context.Container.RegisterSingleton(options);
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.SingleInstance);

            if (!context.Settings.TryGet(out SagaMetadataCollection allSagas))
            {
                return;
            }

            var customFinders = allSagas.SelectMany(sagaMetadata => sagaMetadata.Finders)
                .Where(finder => finder.Properties.ContainsKey("custom-finder-clr-type"))
                .ToArray();

            if (customFinders.Length <= 0)
            {
                return;
            }

            var msg = "RavenDB Persistence does not support custom saga finders using the `IFindSagas<TSagaData>` interface. The following custom finders are invalid:";
            foreach (var finder in customFinders)
            {
                msg += $"{Environment.NewLine}  * {finder.Type.FullName}";
            }

            throw new Exception(msg);
        }
    }
}