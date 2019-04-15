namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using Raven.Client;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;
    using Sparrow.Json;

    static class UnwrappedSagaListener
    {
        static readonly string ContainerTypeName = typeof(SagaDataContainer).FullName + ", NServiceBus.RavenDB";

        public static void Register(DocumentStore store)
        {
            var normalFindIdentityProperty = store.Conventions.FindIdentityProperty;

            // TODO: Not cool that we MUST do this before Initialize(). Need to have a test for that, and perhaps a method that will make our modifications to the store before the host app calls Initialize
            store.Conventions.FindIdentityProperty = memberInfo =>
            {
                if (typeof(IContainSagaData).IsAssignableFrom(memberInfo.DeclaringType))
                {
                    return false;
                }

                return normalFindIdentityProperty(memberInfo);
            };

            store.OnBeforeConversionToEntity += OnBeforeConversionToEntity;
        }

        static void OnBeforeConversionToEntity(object sender, BeforeConversionToEntityEventArgs args)
        {
            if (args.Type == typeof(SagaDataContainer))
            {
                if (!args.Document.TryGetMember("Originator", out _))
                {
                    // The SagaDataContainer will not have "Originator" but older stored IContainSagaData will
                    return;
                }

                if (args.Document.TryGetMember("@metadata", out var metadataObj))
                {
                    if (metadataObj is BlittableJsonReaderObject metadata && metadata.TryGetMember(Constants.Documents.Metadata.RavenClrType, out var lazyClrType) && metadata.TryGetMember(Constants.Documents.Metadata.Id, out var lazyId))
                    {
                        if (lazyClrType is LazyStringValue clrType && lazyId is LazyStringValue)
                        {
                            if (clrType.ToString() != ContainerTypeName)
                            {
                                var id = lazyId.ToString();
                                var sagaType = Type.GetType(clrType.ToString());

                                var sagaData = args.Session.DocumentStore.Conventions.DeserializeEntityFromBlittable(sagaType, args.Document) as IContainSagaData;

                                var container = new SagaDataContainer
                                {
                                    Id = id,
                                    IdentityDocId = null,
                                    Data = sagaData
                                };

                                var documentInfo = new DocumentInfo();
                                documentInfo.Metadata = metadata;

                                args.Document = args.Session.EntityToBlittable.ConvertEntityToBlittable(container, documentInfo);
                            }
                        }
                    }
                }
            }
        }
    }
}
