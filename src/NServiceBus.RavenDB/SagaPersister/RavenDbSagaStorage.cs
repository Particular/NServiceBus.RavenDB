namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using NServiceBus.SagaPersisters.RavenDB;
    using NServiceBus.Sagas;

    /// <summary>
    ///     RavenDB Saga Storage.
    /// </summary>
    public class RavenDbSagaStorage : Feature
    {
        /// <summary>
        ///     Creates an instance of <see cref="RavenDbSagaStorage" />.
        /// </summary>
        internal RavenDbSagaStorage()
        {
            DependsOn<Sagas>();
            RegisterStartupTask<EnsureNoMultiMappedSagas>();
        }

        /// <summary>
        ///     Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            // TODO here would be the place to wire up the ISagaFinder extension point

            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }

        class EnsureNoMultiMappedSagas : FeatureStartupTask
        {
            public EnsureNoMultiMappedSagas(SagaMetadataCollection sagaMetadataCollection)
            {
                this.sagaMetadataCollection = sagaMetadataCollection;
            }

            protected override void OnStart()
            {
                var sagasWithMultipleCorrProps = sagaMetadataCollection.Where(m => m.CorrelationProperties.Count > 1).ToList();

                if (sagasWithMultipleCorrProps.Any())
                {
                    var sagas = string.Join(",", sagasWithMultipleCorrProps.Select(s => s.Name));

                    throw new Exception($"The following sagas have multiple correlation properties, '{sagas}'. Sagas that are correlated on multiple properties are not supported by the RavenDB saga persister.");
                }
            }

            SagaMetadataCollection sagaMetadataCollection;
        }
    }
}