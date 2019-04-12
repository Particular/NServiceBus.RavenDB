namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using Raven.Client;
    using Raven.Client.Documents;

    static class FakeLegacyTimoutDataClrTypeConversionListener
    {
        public static void Install(IDocumentStore store)
        {
            store.OnBeforeStore += (sender, args) =>
            {
                // TODO: Converted from AfterConversionToDocument listener with these commented out statements below, needs testing:
                //metadata[Constants.RavenClrType] = "NServiceBus.TimeoutPersisters.RavenDB.TimeoutData, NServiceBus.RavenDB";
                //metadata[Constants.RavenEntityName] = "TimeoutDatas";
                args.DocumentMetadata[Constants.Documents.Metadata.RavenClrType] = "NServiceBus.TimeoutPersisters.RavenDB.TimeoutData, NServiceBus.RavenDB";
                args.DocumentMetadata[Constants.Documents.Metadata.Collection] = "TimeoutDatas";
            };
        }
    }
}