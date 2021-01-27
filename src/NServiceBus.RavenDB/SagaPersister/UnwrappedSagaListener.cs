namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using Raven.Client;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;
    using Sparrow.Json;
    using Sparrow.Json.Parsing;

    static class UnwrappedSagaListener
    {
        public static void Register(DocumentStore store)
        {
            store.OnBeforeConversionToEntity += OnBeforeConversionToEntity;
            store.OnAfterConversionToEntity += Store_OnAfterConversionToEntity;
        }

        static void Store_OnAfterConversionToEntity(object sender, AfterConversionToEntityEventArgs e)
        {
            // Original v5 to v6 converter did not set the Data.Id value causing it to be Guid.Empty
            if (e.Entity is SagaDataContainer sagaDataContainer && sagaDataContainer.Data.Id == Guid.Empty)
            {
                sagaDataContainer.Data.Id = new Guid(StripSagaIdFromDocumentId(e.Id));
            }
        }

        static void OnBeforeConversionToEntity(object sender, BeforeConversionToEntityEventArgs args)
        {
            if (args.Type != typeof(SagaDataContainer))
            {
                return;
            }

            if (!args.Document.TryGetMember("Originator", out _))
            {
                // The SagaDataContainer will not have "Originator" but older stored IContainSagaData will
                return;
            }

            if (!args.Document.TryGetMember("@metadata", out var metadataObj) ||
                !(metadataObj is BlittableJsonReaderObject metadata) ||
                !metadata.TryGetMember(Constants.Documents.Metadata.RavenClrType, out var lazyClrType) ||
                !(lazyClrType is LazyStringValue clrType) ||
                clrType.ToString() == ContainerTypeName)
            {
                return;
            }

            var sagaType = Type.GetType(clrType.ToString());

            var original = args.Document;

            var sagaData = new DynamicJsonValue(sagaType);
            foreach (var key in original.GetPropertyNames())
            {
                if (key != "@metadata")
                {
                    sagaData[key] = original[key];
                }
            }

            var document = new DynamicJsonValue { ["Id"] = args.Id };
            if (metadata.TryGetWithoutThrowingOnError("NServiceBus-UniqueDocId", out string identityDocId))
            {
                document["IdentityDocId"] = identityDocId;
            }

            sagaData["Id"] = StripSagaIdFromDocumentId(args.Id);

            document["Data"] = sagaData;

            args.Document = args.Session.Context.ReadObject(document, args.Id);
        }

        static string StripSagaIdFromDocumentId(string documentId)
        {
            return documentId.Substring(documentId.IndexOf("/") + 1);
        }

        static readonly string ContainerTypeName = typeof(SagaDataContainer).FullName + ", NServiceBus.RavenDB";
    }


}