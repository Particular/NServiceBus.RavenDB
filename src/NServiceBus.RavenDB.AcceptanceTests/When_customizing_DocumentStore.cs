namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    public class When_customzing_DocStore : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_honor_customized_values()
        {
            var resourceManagerId = Guid.NewGuid();

            var context = await Scenario.Define<Context>(c => c.ExpectedResourceManagerId = resourceManagerId)
                .WithEndpoint<CustomizeDocStoreEndpoint>(b =>
                {
                    b.When(session => session.SendLocal(new TestCmd { Name = "Doesn't matter, let's say Fred" }));

                    b.CustomConfig(cfg =>
                    {
                        ConfigureEndpointRavenDBPersistence.GetDefaultPersistenceExtensions(cfg.GetSettings())
                        .CustomizeDocumentStore(store =>
                        {
                            var ds = store as DocumentStore;
                            ds.Identifier = "TestIdentifier";
                            ds.ResourceManagerId = resourceManagerId;
                            ds.TransactionRecoveryStorage = new VolatileOnlyTransactionRecoveryStorage();
                        });
                    });
                })
                .Done(c => c.MessageReceived)
                .Run();

            Assert.IsNotNull(context.DocStore);
            Assert.AreEqual("TestIdentifier", context.DocStore.Identifier);
            Assert.AreEqual(context.ExpectedResourceManagerId, context.DocStore.ResourceManagerId);
            Assert.IsInstanceOf<VolatileOnlyTransactionRecoveryStorage>(context.DocStore.TransactionRecoveryStorage);
        }

        [Test]
        public async Task Should_not_honor_values_direct_on_DocStore()
        {
            var resourceManagerId = Guid.NewGuid();

            var context = await Scenario.Define<Context>(c => c.ExpectedResourceManagerId = resourceManagerId)
                .WithEndpoint<CustomizeDocStoreEndpoint>(b =>
                {
                    b.When(session => session.SendLocal(new TestCmd { Name = "Doesn't matter, let's say George" }));

                    b.CustomConfig(cfg =>
                    {
                        var ds = ConfigureEndpointRavenDBPersistence.GetDefaultDocumentStore(cfg.GetSettings()) as DocumentStore;
                        ds.ResourceManagerId = resourceManagerId;
                        ds.TransactionRecoveryStorage = new IsolatedStorageTransactionRecoveryStorage();
                    });
                })
                .Done(c => c.MessageReceived)
                .Run();

            Assert.IsNotNull(context.DocStore);
            Assert.AreNotEqual(context.ExpectedResourceManagerId, context.DocStore.ResourceManagerId);
            Assert.IsInstanceOf<LocalDirectoryTransactionRecoveryStorage>(context.DocStore.TransactionRecoveryStorage);
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public Guid ExpectedResourceManagerId { get; set; }
            public DocumentStore DocStore { get; set; }
        }

        public class CustomizeDocStoreEndpoint : EndpointConfigurationBuilder
        {
            public CustomizeDocStoreEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestCustomizeDocStoreSaga : Saga<CustomizeDocStoreSagaData>,
                IAmStartedByMessages<TestCmd>
            {
                public Context Context { get; set; }

                public Task Handle(TestCmd message, IMessageHandlerContext context)
                {
                    this.Data.Name = message.Name;

                    Context.DocStore = context.SynchronizedStorageSession.RavenSession().Advanced.DocumentStore as DocumentStore;
                    Context.MessageReceived = true;

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CustomizeDocStoreSagaData> mapper)
                {
                    mapper.ConfigureMapping<TestCmd>(msg => msg.Name).ToSaga(saga => saga.Name);
                }
            }

            public class CustomizeDocStoreSagaData : ContainSagaData
            {
                public virtual string Name { get; set; }
            }
        }

        public class TestCmd : ICommand
        {
            public string Name { get; set; }
        }
    }

}