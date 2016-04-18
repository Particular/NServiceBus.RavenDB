namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    public class When_specify_ConnectionParameters : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ConnectionParamsEndpoint>(b =>
                {
                    b.When(bus => bus.SendLocal(new TestCmd { Name = "Doesn't matter, let's say George" }));

                    b.CustomConfig((cfg, c) =>
                    {
                        ConfigureEndpointRavenDBPersistence.UseConnectionParameters(cfg.GetSettings());
                    });
                })
                .Done(c => c.MessageReceived)
                .Run();

            Assert.IsNotNull(context.DocStore);
            Assert.IsInstanceOf<IsolatedStorageTransactionRecoveryStorage>(context.DocStore.TransactionRecoveryStorage);
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public DocumentStore DocStore { get; set; }
        }

        public class ConnectionParamsEndpoint : EndpointConfigurationBuilder
        {
            public ConnectionParamsEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestCustomizeDocStoreSaga : Saga<CustomizeDocStoreSagaData>,
                IAmStartedByMessages<TestCmd>
            {
                public Context Context { get; set; }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CustomizeDocStoreSagaData> mapper)
                {
                    mapper.ConfigureMapping<TestCmd>(msg => msg.Name).ToSaga(saga => saga.Name);
                }

                public Task Handle(TestCmd message, IMessageHandlerContext context)
                {
                    Data.Name = message.Name;

                    Context.DocStore = context.SynchronizedStorageSession.RavenSession().Advanced.DocumentStore as DocumentStore;
                    Context.MessageReceived = true;

                    return Task.FromResult(0);
                }
            }

            public class CustomizeDocStoreSagaData : ContainSagaData
            {
                public virtual string Name { get; set; }
            }
        }

        [Serializable]
        public class TestCmd : ICommand
        {
            public string Name { get; set; }
        }
    }

}