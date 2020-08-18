namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

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
                    .SetDefaultDocumentStore((_, __) =>
                    {
                        Assert.Fail("Document store resolved to early");

                        return null;
                    });

            EndpointWithExternallyManagedContainer.Create(endpointConfiguration, new ServiceCollection());
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
    }
}