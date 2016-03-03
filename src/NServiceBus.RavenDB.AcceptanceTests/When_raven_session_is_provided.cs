namespace NServiceBus.AcceptanceTests.ApiExtension
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;
    using Raven.Client;

    public class When_raven_session_is_provided : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task It_should_return_configured_session()
        {
            var context =
            await Scenario.Define<RavenSessionTestContext>()
                .WithEndpoint<SharedRavenSessionExtensions>(builder =>
                {
                    builder.When((msgSession, ctx) =>
                    {
                        var sendOptions = new SendOptions();

                        sendOptions.RouteToThisEndpoint();

                        return msgSession.Send(new SharedRavenSessionExtensions.GenericMessage(), sendOptions);
                    });
                })
                .Done(c => c.HandlerWasHit)
                .Run();

            Assert.AreSame(context.RavenSessionFromTest, context.RavenSessionFromHandler);
            Assert.AreEqual(1, context.SessionCreateCount);
        }

        public class RavenSessionTestContext : ScenarioContext
        {
            public IAsyncDocumentSession RavenSessionFromTest { get; set; }
            public IAsyncDocumentSession RavenSessionFromHandler { get; set; }
            public bool HandlerWasHit { get; set; }

            int _sessionCreateCount;
            public int SessionCreateCount { get { return _sessionCreateCount; } }

            public void IncrementSessionCreateCount()
            {
                Interlocked.Increment(ref _sessionCreateCount);
            }
        }

        public class SharedRavenSessionExtensions : EndpointConfigurationBuilder
        {
            public SharedRavenSessionExtensions()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var scenarioContext = context.ScenarioContext as RavenSessionTestContext;
                    var docStore = ConfigureEndpointRavenDBPersistence.GetDefaultDocumentStore(config.GetSettings());

                    ConfigureEndpointRavenDBPersistence.GetDefaultPersistenceExtensions(config.GetSettings())
                        .UseSharedAsyncSession(() =>
                        {
                            scenarioContext.RavenSessionFromTest = docStore.OpenAsyncSession();
                            scenarioContext.IncrementSessionCreateCount();
                            return scenarioContext.RavenSessionFromTest;
                        });
                });
            }

            public class SharedSessionSagaData : IContainSagaData
            {
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
            }

            public class SharedSessionGenericSaga : Saga<SharedSessionSagaData>, IAmStartedByMessages<GenericMessage>
            {
                public RavenSessionTestContext TestContext { get; set; }

                public Task Handle(GenericMessage message, IMessageHandlerContext context)
                {
                    TestContext.RavenSessionFromHandler = context.SynchronizedStorageSession.RavenSession();
                    TestContext.HandlerWasHit = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SharedSessionSagaData> mapper)
                {
                    mapper.ConfigureMapping<GenericMessage>(m => m.Id).ToSaga(s => s.Id);
                }
            }

            [Serializable]
            public class GenericMessage : IMessage
            {
                public Guid Id { get; set; }
            }
        }
    }
}
