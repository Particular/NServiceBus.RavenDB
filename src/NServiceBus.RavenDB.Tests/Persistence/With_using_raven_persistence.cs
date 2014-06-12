using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class With_using_raven_persistence
{
    [Test]
    [Ignore("Not sure how to test this now with Features")]
    public void Should_not_register_IDocumentStore_into_the_container()
    {
        var config = Configure.With(_ =>
                                    {
                                        _.AssembliesToScan(new[]
                                                           {
                                                               GetType().Assembly
                                                           });
                                        _.EndpointName("UnitTests");
                                    })
            .UsePersistence<RavenDB>();

        config.CreateBus();

        Assert.IsFalse(config.Configurer.HasComponent<IDocumentStore>());
    }

    [Test]
    [Ignore("Not sure how to test this now with Features")]
    public void Features_should_create_default_documentStore()
    {
        var config = Configure.With(_ =>
                                    {
                                        _.AssembliesToScan(new[]
                                                           {
                                                               GetType().Assembly
                                                           });
                                        _.EndpointName("UnitTests");
                                    })
            .UsePersistence<RavenDB>();

        config.CreateBus();

        var documentStore = SharedDocumentStore.Get(config.Settings);
        Assert.NotNull(documentStore);
    }
}