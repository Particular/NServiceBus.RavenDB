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

    public class When_setting_DTC_options_by_convention : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work_when_custom_config_used_directly()
        {
            var context = await Scenario.Define<Context>(c =>
            {
                c.ExecuteConfigClass = false;
                c.ExpectedResourceManagerId = Guid.NewGuid();
                c.TxRecoveryPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\NServiceBus.RavenDB\{c.ExpectedResourceManagerId}";
            })
                .WithEndpoint<SetupDtcEndpoint>(b =>
                {
                    b.When(bus => bus.SendLocal(new TestCmd { Name = "Doesn't matter, let's say George" }));

                    b.CustomConfig((cfg, c) =>
                    {
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

                                store.ResourceManagerId = c.ExpectedResourceManagerId;
                                store.TransactionRecoveryStorage = new TestTxRecoveryStorage(c.TxRecoveryPath);

                                return store;
                            });
                    });
                })
                .Done(c => c.MessageReceived)
                .Run();

            Assert.AreEqual(context.ExpectedResourceManagerId, context.ObservedResourceManagerId);
            Assert.AreEqual(typeof(TestTxRecoveryStorage), context.TxRecoveryType);
        }

        [Test]
        public async Task Should_work_using_INeedInitialization()
        {
            var context = await Scenario.Define<Context>(c =>
            {
                c.ExpectedResourceManagerId = Guid.NewGuid();
                c.TxRecoveryPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\NServiceBus.RavenDB\{c.ExpectedResourceManagerId}";
            })
                .WithEndpoint<SetupDtcEndpoint>(b =>
                {
                    b.When(bus => bus.SendLocal(new TestCmd { Name = "Doesn't matter, let's say George" }));

                    b.CustomConfig((config, c) =>
                    {
                        var settings = config.GetSettings();
                        settings.Set("$RunINeedInitialization", true);
                        settings.Set("$ResourceManagerId", c.ExpectedResourceManagerId);
                        settings.Set("$TxRecoveryPath", c.TxRecoveryPath);
                    });
                })
                .Done(c => c.MessageReceived)
                .Run();

            Assert.AreEqual(context.ExpectedResourceManagerId, context.ObservedResourceManagerId);
            Assert.AreEqual(typeof(TestTxRecoveryStorage), context.TxRecoveryType);
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public Guid ExpectedResourceManagerId { get; set; }
            public Guid ObservedResourceManagerId { get; set; }
            public string TxRecoveryPath { get; set; }
            public Type TxRecoveryType { get; set; }
            public bool ExecuteConfigClass { get; set; }
        }

        public class SetupDtcEndpoint : EndpointConfigurationBuilder
        {
            public SetupDtcEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SniffDtcSettingsSaga : Saga<SniffDtcSettingsSagaData>,
                IAmStartedByMessages<TestCmd>
            {
                public Context Context { get; set; }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SniffDtcSettingsSagaData> mapper)
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

                    return Task.FromResult(0);
                }
            }

            public class SniffDtcSettingsSagaData : ContainSagaData
            {
                public virtual string Name { get; set; }
            }

            class Initializer : INeedInitialization
            {
                public void Customize(EndpointConfiguration configuration)
                {
                    var settings = configuration.GetSettings();
                    if(!settings.HasSetting("$RunINeedInitialization"))
                    {
                        return;
                    }

                    var resourceMgrId = settings.Get<Guid>("$ResourceManagerId");
                    var txRecoveryPath = settings.Get<string>("$TxRecoveryPath");

                    TestDatabaseInfo dbInfo;
                    configuration.UsePersistence<RavenDBPersistence>()
                        .ResetDocumentStoreSettings(out dbInfo)
                        .SetDefaultDocumentStore(readOnlySettings =>
                        {
                            var store = new DocumentStore
                            {
                                Url = dbInfo.Url,
                                DefaultDatabase = dbInfo.DatabaseName
                            };

                            store.ResourceManagerId = resourceMgrId;
                            store.TransactionRecoveryStorage = new TestTxRecoveryStorage(txRecoveryPath);

                            return store;
                        });
                }
            }
        }

        [Serializable]
        public class TestCmd : ICommand
        {
            public string Name { get; set; }
        }

        public class TestTxRecoveryStorage : LocalDirectoryTransactionRecoveryStorage
        {
            public TestTxRecoveryStorage(string path)
                : base(path)
            {
            }
        }
    }

}