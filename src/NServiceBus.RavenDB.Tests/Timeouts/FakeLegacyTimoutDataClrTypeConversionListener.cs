namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using Newtonsoft.Json.Linq;
    using Raven.Client;

    class FakeLegacyTimoutDataClrTypeConversionListener : IDocumentConversionListener
    {
        public void BeforeConversionToDocument(string key, object entity, JObject metadata)
        {
        }

        public void AfterConversionToDocument(string key, object entity, JObject document, JObject metadata)
        {
            metadata[Constants.RavenClrType] = "NServiceBus.TimeoutPersisters.RavenDB.TimeoutData, NServiceBus.RavenDB";
            metadata[Constants.RavenEntityName] = "TimeoutDatas";
        }

        public void BeforeConversionToEntity(string key, JObject document, JObject metadata)
        {
        }

        public void AfterConversionToEntity(string key, JObject document, JObject metadata, object entity)
        {
        }
    }
}