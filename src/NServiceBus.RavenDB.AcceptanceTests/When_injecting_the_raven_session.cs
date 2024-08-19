namespace NServiceBus.AcceptanceTests.ApiExtension
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;

    public class When_injecting_the_raven_session : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task It_should_return_configured_session()
        {
            DocumentStore documentStore = null;
            IAsyncDocumentSession session = null;
            try
            {
                documentStore = await ConfigureEndpointRavenDBPersistence.GetDocumentStore();
                session = documentStore.OpenAsyncSession();

                RavenSessionTestContext context =
                    await Scenario.Define<RavenSessionTestContext>()
                        .WithEndpoint<SharedRavenSessionExtensions>(b =>
                            b.CustomConfig(config =>
                                {
                                    config.UsePersistence<RavenDBPersistence>().UseSharedAsyncSession(_ => session);
                                })
                                .When((bus, c) =>
                                {
                                    var sendOptions = new SendOptions();

                                    sendOptions.RouteToThisEndpoint();

                                    return bus.Send(new SharedRavenSessionExtensions.GenericMessage(), sendOptions);
                                }))
                        .Done(c => c.HandlerWasHit)
                        .Run();

                Assert.That(context.RavenSessionFromHandler, Is.SameAs(session));
            }
            finally
            {
                if (session != null)
                {
                    session.Dispose();
                    session = null;
                }

                if (documentStore != null)
                {
                    await ConfigureEndpointRavenDBPersistence.DeleteDatabase(documentStore.Database);
                }
            }
        }

        [Test]
        public async Task It_should_return_a_new_one_when_none_was_configured()
        {
            DocumentStore documentStore = null;
            try
            {
                documentStore = await ConfigureEndpointRavenDBPersistence.GetDocumentStore();

                RavenSessionTestContext context =
                    await Scenario.Define<RavenSessionTestContext>(testContext => { })
                        .WithEndpoint<SharedRavenSessionExtensions>(b => b.When((bus, c) =>
                        {
                            var sendOptions = new SendOptions();

                            sendOptions.RouteToThisEndpoint();

                            return bus.Send(new SharedRavenSessionExtensions.GenericMessage(), sendOptions);
                        }))
                        .Done(c => c.HandlerWasHit)
                        .Run();

                Assert.IsNotNull(context.RavenSessionFromHandler);
            }
            finally
            {
                if (documentStore != null)
                {
                    await ConfigureEndpointRavenDBPersistence.DeleteDatabase(documentStore.Database);
                }
            }
        }

        public class RavenSessionTestContext : ScenarioContext
        {
            public IAsyncDocumentSession RavenSessionFromHandler { get; set; }
            public bool HandlerWasHit { get; set; }
        }

        public class SharedRavenSessionExtensions : EndpointConfigurationBuilder
        {
            public SharedRavenSessionExtensions() => EndpointSetup<DefaultServer>();

            public class SharedSessionSagaData : IContainSagaData
            {
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
            }

            public class SharedSessionGenericSaga : Saga<SharedSessionSagaData>, IAmStartedByMessages<GenericMessage>
            {
                readonly IAsyncDocumentSession ravenSession;
                readonly RavenSessionTestContext testContext;

                public SharedSessionGenericSaga(RavenSessionTestContext testContext, IAsyncDocumentSession ravenSession)
                {
                    this.testContext = testContext;
                    this.ravenSession = ravenSession;
                }

                public Task Handle(GenericMessage message, IMessageHandlerContext context)
                {
                    testContext.RavenSessionFromHandler = ravenSession;
                    testContext.HandlerWasHit = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SharedSessionSagaData> mapper) =>
                    mapper.ConfigureMapping<GenericMessage>(m => m.Id).ToSaga(s => s.Id);
            }

            [Serializable]
            public class GenericMessage : IMessage
            {
                public Guid Id { get; set; }
            }
        }
    }
}