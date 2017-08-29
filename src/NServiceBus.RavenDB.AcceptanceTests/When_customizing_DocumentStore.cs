namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NUnit.Framework;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    public class When_customzing_DocStore : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_change_tx_recovery_values_for_provided_DocStore_with_IsolatedStorage()
        {
            var context = await Scenario.Define<Context>(testContext => { testContext.ExpectedResourceManagerId = Guid.NewGuid(); })
                .WithEndpoint<CustomizeDocStoreEndpoint>(b =>
                {
                    b.When(bus => bus.SendLocal(new TestCmd { Name = "Doesn't matter, let's say George" }));

                    b.CustomConfig((cfg, c) =>
                    {
                        var ds = ConfigureEndpointRavenDBPersistence.GetDefaultDocumentStore(cfg.GetSettings());
                        ds.ResourceManagerId = c.ExpectedResourceManagerId;
                        ds.TransactionRecoveryStorage = new IsolatedStorageTransactionRecoveryStorage();
                    });
                })
                .Done(c => c.MessageReceived)
                .Run();

            Assert.IsNotNull(context.DocStore);
            Assert.AreEqual(context.ExpectedResourceManagerId, context.DocStore.ResourceManagerId);
            Assert.IsInstanceOf<IsolatedStorageTransactionRecoveryStorage>(context.DocStore.TransactionRecoveryStorage);
        }

        [Test]
        public async Task Should_not_change_tx_recovery_values_for_provided_DocStore_with_LocalDirectoryStorage()
        {

            var context = await Scenario.Define<Context>(testContext => { testContext.ExpectedResourceManagerId = Guid.NewGuid(); })
                .WithEndpoint<CustomizeDocStoreEndpoint>(b =>
                {
                    b.When(bus => bus.SendLocal(new TestCmd { Name = "Doesn't matter, let's say George" }));

                    b.CustomConfig((cfg, c) =>
                    {
                        var ds = ConfigureEndpointRavenDBPersistence.GetDefaultDocumentStore(cfg.GetSettings());
                        ds.ResourceManagerId = c.ExpectedResourceManagerId;
                        // ConfigureRavenDBPersistence already sets LocalDirectoryTransactionRecoveryStorage
                    });
                })
                .Done(c => c.MessageReceived)
                .Run();

            Assert.IsNotNull(context.DocStore);
            Assert.AreEqual(context.ExpectedResourceManagerId, context.DocStore.ResourceManagerId);
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