namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.TimeoutPersisters.RavenDB;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;

    static class TimeoutDataV1toV2Converter
    {
        public static void Register(DocumentStore store)
        {
            store.OnAfterConversionToEntity += OnAfterConversionToEntity;
        }

        static void OnAfterConversionToEntity(object sender, AfterConversionToEntityEventArgs args)
        {
            if (args.Entity is TimeoutData timeoutData)
            {
                var realDestination = LegacyAddress.ParseToString(() => timeoutData.Destination);
                timeoutData.Destination = realDestination;
            }
            else if (args.Entity is Timeout.Core.TimeoutData coreTimeout)
            {
                var realDestination = LegacyAddress.ParseToString(() => coreTimeout.Destination);
                coreTimeout.Destination = realDestination;
            }
        }

        // TODO: Did changing from BeforeConversionToEntity to AfterConversionToEntity lose anything important from the IsTimeoutData method below?

        //static bool IsTimeoutData(JObject ravenJObject)
        //{
        //    //metadata can be null when loading data using a transformers
        //    var clrMetaData = ravenJObject?["Raven-Clr-Type"];
        //    //not all raven document types have 'Raven-Clr-Type' metadata
        //    if (clrMetaData == null)
        //    {
        //        return false;
        //    }
        //    var clrType = clrMetaData.Value<string>();
        //    return !string.IsNullOrEmpty(clrType) &&
        //        clrType == "NServiceBus.TimeoutPersisters.RavenDB.TimeoutData, NServiceBus.RavenDB";
        //}
    }
}