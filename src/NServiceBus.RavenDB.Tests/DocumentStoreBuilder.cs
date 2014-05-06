using System.IO;
using Raven.Client.Document;
using Raven.Client.Embedded;

public class DocumentStoreBuilder
{
    public static EmbeddableDocumentStore Build()
    {
        var store = new EmbeddableDocumentStore
            {
                RunInMemory = true,
                Conventions =
                    {
                        DefaultQueryingConsistency = ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite,
                    }
            };
        store.Configuration.RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true;
        store.Configuration.CompiledIndexCacheDirectory = Path.GetTempPath();

        store.Initialize();
        return store;
    }
}