using NServiceBus;
using NServiceBus.RavenDB;
using NServiceBus.RavenDB.Persistence;
using NUnit.Framework;
using Raven.Client.Document;

[TestFixture]
public class When_configuring_raven_persistence
{

    [Test]
    public void Ensure_defaults_are_set()
    {
        var config = Configure.With(new[]
            {
                GetType().Assembly
            })
            .DefineEndpointName("UnitTests")
            .DefaultBuilder();

        using (var store = new DocumentStore())
        {
            config.RavenDBPersistence(store);

            Assert.AreEqual("http://localhost:8080", store.Url);
            Assert.AreEqual("UnitTests", store.DefaultDatabase);
            Assert.AreEqual(RavenPersistenceConstants.DefaultResourceManagerId, store.ResourceManagerId);
            Assert.AreEqual("NServiceBus.RavenDB.Persistence.RavenConventions.FindTypeTagName", store.Conventions.FindTypeTagName.Method.DeclaringType.FullName + "." + store.Conventions.FindTypeTagName.Method.Name);
            Assert.AreEqual(typeof(NoOpLogManager), Raven.Abstractions.Logging.LogManager.CurrentLogManager.GetType());

            Assert.IsTrue(config.Configurer.HasComponent<StoreAccessor>());
            Assert.IsTrue(config.Configurer.HasComponent<RavenSessionFactory>());
            Assert.IsTrue(config.Configurer.HasComponent<RavenUnitOfWork>());
        }

    }


}