namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NUnit.Framework;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    public class When_disabling_DTC : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work_when_custom_config_used_directly()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<DisableDtcEndpoint>(b =>
                {
                    b.When(bus => bus.SendLocal(new TestCmd { Name = "Doesn't matter, let's say George" }));

                    b.CustomConfig((cfg, c) =>
                    {
                        var transportExtensions = new TransportExtensions(cfg.GetSettings());
                        transportExtensions.Transactions(TransportTransactionMode.ReceiveOnly);

                        TestDatabaseInfo dbInfo;
                        cfg.UsePersistence<RavenDBPersistence>()
                            .ResetDocumentStoreSettings(out dbInfo)
                            .SetDefaultDocumentStore(settings =>
                            {
                                var store = new DocumentStore
                                {
                                    Url = dbInfo.Url,
                                    DefaultDatabase = dbInfo.DatabaseName
                                };

                                return store;
                            });
                    });
                })
                .Done(c => c.MessageReceived)
                .Run();
            
            Assert.AreNotEqual(Guid.Empty, context.ObservedResourceManagerId);
            Assert.AreEqual(false, context.DocumentStoreEnlistsInDtc);
            Assert.AreEqual(typeof(VolatileOnlyTransactionRecoveryStorage), context.TxRecoveryType);
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public Guid ObservedResourceManagerId { get; set; }
            public Type TxRecoveryType { get; set; }
            public bool? DocumentStoreEnlistsInDtc { get; set; }
        }

        public class DisableDtcEndpoint : EndpointConfigurationBuilder
        {
            public DisableDtcEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class VerifyDtcDisabledSaga : Saga<VerifyDtcDisabledSagaData>,
                IAmStartedByMessages<TestCmd>
            {
                public Context Context { get; set; }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<VerifyDtcDisabledSagaData> mapper)
                {
                    mapper.ConfigureMapping<TestCmd>(msg => msg.Name).ToSaga(saga => saga.Name);
                }

                public Task Handle(TestCmd message, IMessageHandlerContext context)
                {
                    Data.Name = message.Name;

                    var docStore = context.SynchronizedStorageSession.RavenSession().Advanced.DocumentStore as DocumentStore;

                    Context.MessageReceived = true;
                    Context.ObservedResourceManagerId = docStore.ResourceManagerId;
                    Context.TxRecoveryType = docStore.TransactionRecoveryStorage.GetType();
                    Context.DocumentStoreEnlistsInDtc = docStore.EnlistInDistributedTransactions;

                    return Task.FromResult(0);
                }
            }

            public class VerifyDtcDisabledSagaData : ContainSagaData
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