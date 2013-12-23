using Mono.CSharp;
using NServiceBus;
using NServiceBus.RavenDB;
using NServiceBus.RavenDB.Persistence;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;

[TestFixture]
public class With_using_raven_persistence
{
    [Test]
    public void Should_not_register_IDocumentStore_into_the_container()
    {
        var config = Configure.With(new[]
            {
                GetType().Assembly
            })
            .DefineEndpointName("UnitTests")
            .DefaultBuilder()
            .RavenPersistence();

        Assert.IsFalse(config.Configurer.HasComponent<IDocumentStore>());
    }

    [Test]
    public void Should_enable_the_user_to_take_control_over_the_session_lifecycle()
    {
        var config = Configure.With(new[]
            {
                GetType().Assembly
            })
            .DefineEndpointName("UnitTests")
            .DefaultBuilder()
            .RavenDBStorageWithSelfManagedSession(new DocumentStore(),false,()=>null);

        
        Assert.IsFalse(config.Configurer.HasComponent<RavenSessionFactory>());
        Assert.IsFalse(config.Configurer.HasComponent<RavenUnitOfWork>());
    }
}