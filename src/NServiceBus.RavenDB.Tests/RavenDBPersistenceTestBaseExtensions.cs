namespace NServiceBus.RavenDB.Tests
{
    using NServiceBus.Sagas;

    static class RavenDBPersistenceTestBaseExtensions
    {
        public static SagaCorrelationProperty CreateMetadata<T>(this RavenDBPersistenceTestBase test, IContainSagaData sagaEntity)
        {
            _ = test;

            var metadata = SagaMetadata.Create(typeof(T));

            metadata.TryGetCorrelationProperty(out SagaMetadata.CorrelationPropertyMetadata correlationPropertyMetadata);

            var propertyInfo = metadata.SagaEntityType.GetProperty(correlationPropertyMetadata.Name);
            var value = propertyInfo.GetValue(sagaEntity);

            var correlationProperty = new SagaCorrelationProperty(correlationPropertyMetadata.Name, value);

            return correlationProperty;
        }
    }
}