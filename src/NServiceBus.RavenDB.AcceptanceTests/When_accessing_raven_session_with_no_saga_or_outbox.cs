namespace NServiceBus.AcceptanceTests.ApiExtension
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Raven.Client;

    public class When_accessing_raven_session_with_no_saga_or_outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task It_should_throw_an_exception()
        {
            var context =
                await Scenario.Define<RavenSessionTestContext>()
                    .WithEndpoint<RavenSessionExtensions>(b => b.When((bus, c) =>
                    {
                        var options = new SendOptions();

                        options.RouteToLocalEndpointInstance();

                        return bus.Send(new RavenSessionExtensions.GenericMessage(), options);
                    }))
                    .Done(c => c.HandlerWasHit)
                    .Run();

            Assert.IsNull(context.RavenSessionFromHandler);
            Assert.IsTrue(context.Exception.Message.ToLower().Contains("saga"), "The exception message should alert the user about necessary features.");
            Assert.IsTrue(context.Exception.Message.ToLower().Contains("outbox"), "The exception message should alert the user about necessary features.");
        }

        public class RavenSessionTestContext : ScenarioContext
        {
            public IAsyncDocumentSession RavenSessionFromTest { get; set; }
            public IAsyncDocumentSession RavenSessionFromHandler { get; set; }
            public bool HandlerWasHit { get; set; }
            public Exception Exception { get; set; }
        }

        public class RavenSessionExtensions : EndpointConfigurationBuilder
        {
            public RavenSessionExtensions()
            {
                EndpointSetup<DefaultServer>();
            }

            public class GenericMessageHandler : IHandleMessages<GenericMessage>
            {
                public RavenSessionTestContext TestContext { get; set; }

                public Task Handle(GenericMessage message, IMessageHandlerContext context)
                {
                    try
                    {
                        TestContext.RavenSessionFromHandler = context.SynchronizedStorageSession.Session();
                    }
                    catch (Exception e)
                    {
                        TestContext.Exception = e;
                    }
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
