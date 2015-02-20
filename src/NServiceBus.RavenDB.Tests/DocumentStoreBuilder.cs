using System.IO;
using Raven.Client.Document;
using Raven.Client.Embedded;

public class DocumentStoreBuilder
{
    // TODO deprecate in favor of using the one in RavenTestBase
    public static EmbeddableDocumentStore Build()
    {
        var store = new EmbeddableDocumentStore
        {
            RunInMemory = true,
            Conventions =
            {
                DefaultQueryingConsistency = ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite,
            },
            Configuration =
            {
                RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true,
                CompiledIndexCacheDirectory = Path.GetTempPath()
            }
        };

        store.Initialize();
        return store;
    }
}