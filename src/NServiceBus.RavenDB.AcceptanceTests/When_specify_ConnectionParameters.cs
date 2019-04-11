namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Persistence.RavenDB;
    using NUnit.Framework;
    using Raven.Client.Documents;

    public class When_specify_ConnectionParameters : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<ConnectionParamsEndpoint>(b =>
                {
                    b.When(bus => bus.SendLocal(new TestCmd { Name = "Doesn't matter, let's say George" }));

                    b.CustomConfig((cfg, c) =>
                    {
                        TestDatabaseInfo dbInfo;

                        cfg.UsePersistence<RavenDBPersistence>()
                            .ResetDocumentStoreSettings(out dbInfo)
                            .SetDefaultDocumentStore(new ConnectionParameters
                            {
                                Url = dbInfo.Url,
                                DatabaseName = dbInfo.DatabaseName,
                                ApiKey = Environment.GetEnvironmentVariable("RavenDbApiKey")
                            });
                    });
                })
                .Done(c => c.MessageReceived)
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public string DocStoreApiKey { get; set; }
        }

        public class ConnectionParamsEndpoint : EndpointConfigurationBuilder
        {
            public ConnectionParamsEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class ConnectionParamsSaga : Saga<ConnectionParamsSagaData>,
                IAmStartedByMessages<TestCmd>
            {
                public Context Context { get; set; }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ConnectionParamsSagaData> mapper)
                {
                    mapper.ConfigureMapping<TestCmd>(msg => msg.Name).ToSaga(saga => saga.Name);
                }

                public Task Handle(TestCmd message, IMessageHandlerContext context)
                {
                    Data.Name = message.Name;

                    var docStore = context.SynchronizedStorageSession.RavenSession().Advanced.DocumentStore as DocumentStore;

                    Context.DocStoreApiKey = docStore.ApiKey;
                    Context.MessageReceived = true;

                    return Task.FromResult(0);
                }
            }

            public class ConnectionParamsSagaData : ContainSagaData
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