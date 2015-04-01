namespace NServiceBus.RavenDB.Tests
{
    using System.Collections.Generic;
    using NServiceBus.Sagas;

    static class RavenDBPersistenceTestBaseExtensions
    {
        public static IDictionary<string,object> CreateMetadata<T>(this RavenDBPersistenceTestBase test, IContainSagaData sagaEntity)
        {
            var metadata = SagaMetadata.Create(typeof(T));
            var result = new Dictionary<string, object>();

            foreach (var correlationProperty in metadata.CorrelationProperties)
            {
                var propertyInfo = metadata.SagaEntityType.GetProperty(correlationProperty.Name);
                var value = propertyInfo.GetValue(sagaEntity);

                result.Add(correlationProperty.Name, value);
            }

            return result;
        }
    }
}