namespace NServiceBus.AcceptanceTests.ApiExtension
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Raven.Client;

    public class When_accessing_raven_session_from_handler_with_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task It_should_return_configured_session()
        {
            var context =
                await Scenario.Define<RavenSessionTestContext>()
                    .WithEndpoint<RavenSessionExtensions>(b => b.When((bus, c) =>
                    {
                        var sendOptions = new SendOptions();

                        sendOptions.RouteToThisEndpoint();

                        return bus.Send(new RavenSessionExtensions.GenericMessage(), sendOptions);
                    }))
                    .Done(c => c.HandlerWasHit)
                    .Run();

            Assert.IsNotNull(context.RavenSessionFromHandler);
        }

        public class RavenSessionTestContext : ScenarioContext
        {
            public IAsyncDocumentSession RavenSessionFromTest { get; set; }
            public IAsyncDocumentSession RavenSessionFromHandler { get; set; }
            public bool HandlerWasHit { get; set; }
        }

        public class RavenSessionExtensions : EndpointConfigurationBuilder
        {
            public RavenSessionExtensions()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SessionExtensionSagaData : IContainSagaData
            {
                public SessionExtensionSagaData()
                {
                    if (Id.ToString() == new Guid().ToString())
                    {
                        Id = Guid.NewGuid();
                    }
                }

                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
            }

            public class SessionExtensionGenericSaga : Saga<SessionExtensionSagaData>, IAmStartedByMessages<GenericMessage>
            {
                public RavenSessionTestContext TestContext { get; set; }

                public Task Handle(GenericMessage message, IMessageHandlerContext context)
                {
                    TestContext.RavenSessionFromHandler = context.SynchronizedStorageSession.RavenSession();
                    TestContext.HandlerWasHit = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SessionExtensionSagaData> mapper)
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
