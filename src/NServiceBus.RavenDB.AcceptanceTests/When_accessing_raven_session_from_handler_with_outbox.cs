namespace NServiceBus.AcceptanceTests.ApiExtension
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;
    using Raven.Client;

    public class When_accessing_raven_session_from_handler_with_outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task It_should_return_configured_session()
        {
            var context =
                await Scenario.Define<RavenSessionTestContext>()
                    .WithEndpoint<RavenSessionExtensionsWithOutbox>(b => b.When((bus, c) =>
                    {
                        var options = new SendOptions();

                        options.RouteToThisEndpoint();

                        return bus.Send(new RavenSessionExtensionsWithOutbox.GenericMessage(), options);
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

        public class RavenSessionExtensionsWithOutbox : EndpointConfigurationBuilder
        {
            public RavenSessionExtensionsWithOutbox()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.GetSettings().Set("DisableOutboxTransportCheck", true);
                    config.EnableOutbox();
                });
            }

            public class GenericMessageHandler : IHandleMessages<GenericMessage>
            {
                public RavenSessionTestContext TestContext { get; set; }

                public Task Handle(GenericMessage message, IMessageHandlerContext context)
                {
                    TestContext.RavenSessionFromHandler = context.SynchronizedStorageSession.RavenSession();
                    TestContext.HandlerWasHit = true;
                    return Task.FromResult(0);
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
