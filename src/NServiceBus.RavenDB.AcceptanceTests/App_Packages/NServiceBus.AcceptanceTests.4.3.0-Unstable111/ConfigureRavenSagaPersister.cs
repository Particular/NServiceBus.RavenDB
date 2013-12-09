using NServiceBus;
using NServiceBus.RavenDB;
using Raven.Client.Document;

public class ConfigureRavenSagaPersister
{
    public void Configure(Configure config)
    {
        var store = new DocumentStore
            {
                Conventions =
                    {
                        DefaultQueryingConsistency = ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite
                    }
            };

        config.RavenDBPersistence(store, true);
    }
}