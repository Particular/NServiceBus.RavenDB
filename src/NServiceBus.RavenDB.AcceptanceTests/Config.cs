using NServiceBus.Persistence;
using Raven.Client;
using Raven.Client.Embedded;
// ReSharper disable UnusedParameter.Global
using NServiceBus;

public abstract class ConfigurePersistences
{
    protected readonly IDocumentStore documentStore;

    protected ConfigurePersistences()
    {
        documentStore = new EmbeddableDocumentStore
                        {
                            RunInMemory = true,
//#if DEBUG
//                            UseEmbeddedHttpServer = true, // enable debugging through HTTP
//#endif
                        };
        documentStore.Initialize();
    }
}

public class ConfigureRavenDBPersistence : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.Settings.Set(RavenDbSettingsExtenstions.DocumentStoreSettingsKey, documentStore);
        // TODO register documentStore to be disposed through NSB Pipeline
    }
}

// TODO these currently don't get picked up because recent config API changes broke ConfigureExtensions.DefinePersistence
//public class ConfigureRavenDBTimeoutPersistence : ConfigurePersistences
//{
//    public void Configure(Configure config)
//    {
//        config.Settings.Set(RavenDbTimeoutSettingsExtenstions.SettingsKey, documentStore);
//    }
//}
//
//public class ConfigureRavenDBSubscriptionPersistence : ConfigurePersistences
//{
//    public void Configure(Configure config)
//    {
//        config.Settings.Set(RavenDbSubscriptionSettingsExtenstions.SettingsKey, documentStore);
//    }
//}
//
//public class ConfigureRavenDBSagaPersister : ConfigurePersistences
//{
//    public void Configure(Configure config)
//    {
//        config.Settings.Set(RavenDbSagaSettingsExtenstions.SettingsKey, documentStore);
//    }
//}