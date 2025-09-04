namespace NServiceBus.AcceptanceTests.ApiExtension
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NUnit.Framework;
    using Raven.Client.Documents.Session;

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

            Assert.That(context.RavenSessionFromHandler, Is.Not.Null);
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
                    config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                    config.EnableOutbox();
                });
            }

            public class GenericMessageHandler : IHandleMessages<GenericMessage>
            {
                RavenSessionTestContext testContext;

                public GenericMessageHandler(RavenSessionTestContext testContext) => this.testContext = testContext;

                public Task Handle(GenericMessage message, IMessageHandlerContext context)
                {
                    testContext.RavenSessionFromHandler = context.SynchronizedStorageSession.RavenSession();
                    testContext.HandlerWasHit = true;
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