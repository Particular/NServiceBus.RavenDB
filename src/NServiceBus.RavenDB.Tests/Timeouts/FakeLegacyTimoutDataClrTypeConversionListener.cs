namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using Raven.Abstractions.Data;
    using Raven.Client.Listeners;
    using Raven.Json.Linq;

    class FakeLegacyTimoutDataClrTypeConversionListener : IDocumentConversionListener
    {
        public void BeforeConversionToDocument(string key, object entity, RavenJObject metadata)
        {
        }

        public void AfterConversionToDocument(string key, object entity, RavenJObject document, RavenJObject metadata)
        {
            metadata[Constants.RavenClrType] = "NServiceBus.TimeoutPersisters.RavenDB.TimeoutData, NServiceBus.RavenDB";
            metadata[Constants.RavenEntityName] = "TimeoutDatas";
        }

        public void BeforeConversionToEntity(string key, RavenJObject document, RavenJObject metadata)
        {
        }

        public void AfterConversionToEntity(string key, RavenJObject document, RavenJObject metadata, object entity)
        {
        }
    }
}