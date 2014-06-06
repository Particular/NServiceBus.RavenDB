using NServiceBus;
using NServiceBus.Persistence;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class With_using_raven_persistence
{
    [Test]
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
            .DefaultBuilder()
            .UsePersistence<RavenDB>();

        Assert.IsFalse(config.Configurer.HasComponent<IDocumentStore>());
    }
}