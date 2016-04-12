﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.Saga;
    using NUnit.Framework;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    public class When_customzing_DocStore : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_change_tx_recovery_values_for_provided_DocStore_with_IsolatedStorage()
        {
            var context = new Context();

            context.ExpectedResourceManagerId = Guid.NewGuid();

            Scenario.Define(context)
                .WithEndpoint<CustomizeDocStoreEndpoint>(b =>
                {
                    b.Given(bus => bus.SendLocal(new TestCmd { Name = "Doesn't matter, let's say George" }));

                    b.CustomConfig(cfg =>
                    {
                        var ds = ConfigureRavenDBPersistence.GetDefaultDocumentStore(cfg.GetSettings()) as DocumentStore;
                        ds.ResourceManagerId = context.ExpectedResourceManagerId;
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
        public void Should_not_change_tx_recovery_values_for_provided_DocStore_with_LocalDirectoryStorage()
        {
            var context = new Context();

            context.ExpectedResourceManagerId = Guid.NewGuid();

            Scenario.Define(context)
                .WithEndpoint<CustomizeDocStoreEndpoint>(b =>
                {
                    b.Given(bus => bus.SendLocal(new TestCmd { Name = "Doesn't matter, let's say George" }));

                    b.CustomConfig(cfg =>
                    {
                        var ds = ConfigureRavenDBPersistence.GetDefaultDocumentStore(cfg.GetSettings()) as DocumentStore;
                        ds.ResourceManagerId = context.ExpectedResourceManagerId;
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
                public ISessionProvider SessionProvider { get; set; }

                public void Handle(TestCmd message)
                {
                    this.Data.Name = message.Name;

                    Context.DocStore = SessionProvider.Session.Advanced.DocumentStore as DocumentStore;
                    Context.MessageReceived = true;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CustomizeDocStoreSagaData> mapper)
                {
                    mapper.ConfigureMapping<TestCmd>(msg => msg.Name).ToSaga(saga => saga.Name);
                }
            }

            public class CustomizeDocStoreSagaData : ContainSagaData
            {
                [Unique]
                public string Name { get; set; }
            }
        }

        public class TestCmd : ICommand
        {
            public string Name { get; set; }
        }
    }

}