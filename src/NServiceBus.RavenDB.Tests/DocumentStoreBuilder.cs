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
                        DefaultQueryingConsistency = ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite
                    }
            };

        store.Initialize();
        return store;
    }
}