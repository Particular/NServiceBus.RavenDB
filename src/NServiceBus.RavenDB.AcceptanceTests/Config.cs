using NServiceBus.Persistence;
using Raven.Client.Embedded;
// ReSharper disable UnusedParameter.Global
using NServiceBus;

public class ConfigureRavenDBPersistence
{
    public void Configure(Configure config)
    {
        config.UsePersistence<RavenDB>(c => c.SetDefaultDocumentStore(new EmbeddableDocumentStore
                                                                      {
                                                                          RunInMemory = true,
                                                                      }.Initialize()));
        // TODO register documentStore to be disposed through NSB Pipeline
    }
}