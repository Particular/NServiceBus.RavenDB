using NServiceBus.Persistence;
using NServiceBus.RavenDB;
using Raven.Client.Embedded;
// ReSharper disable UnusedParameter.Global
using NServiceBus;

public class ConfigureRavenDBPersistence
{
    public void Configure(Configure config)
    {
        config.UsePersistence<RavenDB>(c => c.SetDefaultDocumentStore(new ConnectionParameters { Url = "http://localhost:8080"}));
        // TODO register documentStore to be disposed through NSB Pipeline
    }
}