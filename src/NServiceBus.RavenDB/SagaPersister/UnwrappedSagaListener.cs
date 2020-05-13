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
        static readonly string ContainerTypeName = typeof(SagaDataContainer).FullName + ", NServiceBus.RavenDB";

        public static void Register(DocumentStore store)
        {
            store.OnBeforeConversionToEntity += OnBeforeConversionToEntity;
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

            var document = new DynamicJsonValue();
            document["Id"] = args.Id;
            if (metadata.TryGetWithoutThrowingOnError("NServiceBus-UniqueDocId", out string identityDocId))
            {
                document["IdentityDocId"] = identityDocId;
            }
            document["Data"] = sagaData;



            args.Document = args.Session.Context.ReadObject(document, args.Id);
        }
    }
}
