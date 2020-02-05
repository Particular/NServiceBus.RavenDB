namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using NServiceBus.ObjectBuilder;

    [TestFixture]
    public class When_providing_a_custom_document_store
    {
        [Test]
        public void Should_not_resolve_until_start()
        {
            var endpointConfiguration = new EndpointConfiguration("custom-docstore-endpoint");

            endpointConfiguration.AssemblyScanner().ExcludeAssemblies("NServiceBus.RavenDB.Tests");
            endpointConfiguration.UseTransport<LearningTransport>();
            endpointConfiguration.EnableOutbox();

            endpointConfiguration.UsePersistence<RavenDBPersistence>()
                    .SetDefaultDocumentStore((IBuilder builder) =>
                    {
                        Assert.Fail("Document store resolved to early");

                        return null;
                    });

            EndpointWithExternallyManagedContainer.Create(endpointConfiguration, new FakeContainerRegistration());
        }

        class MySaga : Saga<MySaga.SagaData>, IAmStartedByMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<MyMessage>(m => m.SomeId)
                    .ToSaga(s => s.SomeId);
            }

            public class SagaData : ContainSagaData
            {
                public string SomeId { get; set; }
            }
        }

        class MyMessage : IMessage
        {
            public string SomeId { get; set; }
        }

        class FakeContainerRegistration : IConfigureComponents
        {
            public void ConfigureComponent(Type concreteComponent, DependencyLifecycle dependencyLifecycle)
            {

            }

            public void ConfigureComponent<T>(DependencyLifecycle dependencyLifecycle)
            {

            }

            public void ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
            {

            }

            public void ConfigureComponent<T>(Func<IBuilder, T> componentFactory, DependencyLifecycle dependencyLifecycle)
            {

            }

            public bool HasComponent<T>()
            {
                return false;
            }

            public bool HasComponent(Type componentType)
            {
                return false;
            }

            public void RegisterSingleton(Type lookupType, object instance)
            {

            }

            public void RegisterSingleton<T>(T instance)
            {
            }
        }
    }
}