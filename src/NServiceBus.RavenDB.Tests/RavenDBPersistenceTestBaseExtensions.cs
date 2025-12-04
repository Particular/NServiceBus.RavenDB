namespace NServiceBus.RavenDB.Tests;

using Sagas;

static class RavenDBPersistenceTestBaseExtensions
{
    public static SagaCorrelationProperty CreateMetadata<TSaga>(this RavenDBPersistenceTestBase test, IContainSagaData sagaEntity) where TSaga : Saga
    {
        _ = test;

        var metadata = SagaMetadata.Create<TSaga>();

        metadata.TryGetCorrelationProperty(out SagaMetadata.CorrelationPropertyMetadata correlationPropertyMetadata);

        var propertyInfo = metadata.SagaEntityType.GetProperty(correlationPropertyMetadata.Name);
        var value = propertyInfo.GetValue(sagaEntity);

        var correlationProperty = new SagaCorrelationProperty(correlationPropertyMetadata.Name, value);

        return correlationProperty;
    }
}