namespace NServiceBus.RavenDB.Tests;

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

        endpointConfiguration.AssemblyScanner().ExcludeAssemblies($"{GetType().Assembly.GetName().Name}");
        var transport = new LearningTransport
        {
            TransportTransactionMode = TransportTransactionMode.None,
            StorageDirectory = null,
            RestrictPayloadSize = false
        };
        transport.TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
        endpointConfiguration.UseTransport(transport);
        endpointConfiguration.EnableOutbox();
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

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
            mapper.MapSaga(s => s.SomeId).ToMessage<MyMessage>(m => m.SomeId);
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