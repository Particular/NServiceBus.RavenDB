using NServiceBus;
using Raven.Client;

public abstract class ConfigurePersistences
{
    protected readonly IDocumentStore documentStore;

    protected ConfigurePersistences()
    {
//        documentStore = new EmbeddableDocumentStore
//                        {
//                            RunInMemory = true,
//                            UseEmbeddedHttpServer = true, // enable debugging through HTTP
//                        }.Initialize();
    }
}

public class ConfigureRavenTimeoutPersistence : ConfigurePersistences
{
    public void Configure(Configure config)
    {
    }
}

public class ConfigureRavenSubscriptionPersistence : ConfigurePersistences
{
    public void Configure(Configure config)
    {
    }
}

public class ConfigureRavenSagaPersister : ConfigurePersistences
{
    public void Configure(Configure config)
    {
    }
}