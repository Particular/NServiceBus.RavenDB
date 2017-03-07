namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Threading.Tasks;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Json.Linq;
    using Raven.Tests.Helpers;

    static class RavenUtils
    {
        public static async Task StoreAsType(IDocumentStore store, string documentId, Type storeAsType, object document)
        {
            var idProperty = document.GetType().GetProperty("Id");
            if (idProperty != null && idProperty.PropertyType == typeof(string))
            {
                var existing = idProperty.GetValue(document) as string;
                if (existing == null)
                {
                    idProperty.SetValue(document, documentId);
                }
            }

            var tagName = store.Conventions.FindTypeTagName(storeAsType);
            var docJson = RavenJObject.FromObject(document);
            var metadata = new RavenJObject();
            metadata["Raven-Entity-Name"] = tagName;
            metadata["Raven-Clr-Type"] = storeAsType.AssemblyQualifiedName;


            await store.AsyncDatabaseCommands.PutAsync(documentId, Etag.Empty, docJson, metadata);

            RavenTestBase.WaitForIndexing(store);
        }
    }
}
