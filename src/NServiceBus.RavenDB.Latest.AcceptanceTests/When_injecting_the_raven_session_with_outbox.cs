namespace NServiceBus.AcceptanceTests.ApiExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;

    public class When_injecting_the_raven_session_with_outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task It_should_return_configured_session()
        {
            DocumentStore documentStore = null;
            var createdSessions = new List<IAsyncDocumentSession>();
            try
            {
                documentStore = await ConfigureEndpointRavenDBPersistence.GetDocumentStore();

                RavenSessionTestContext context =
                    await Scenario.Define<RavenSessionTestContext>()
                        .WithEndpoint<SharedRavenSessionExtensions>(b =>
                            b.CustomConfig(config =>
                                {
                                    var persistence = config.UsePersistence<RavenDBPersistence>();
                                    persistence.UseSharedAsyncSession(_ =>
                                    {
                                        var useClusterWideTx = persistence.GetSettings().GetOrDefault<bool>("NServiceBus.Persistence.RavenDB.EnableClusterWideTransactions");
                                        var sessionOptions = new SessionOptions
                                        {
                                            TransactionMode = useClusterWideTx ? TransactionMode.ClusterWide : TransactionMode.SingleNode
                                        };
                                        var session = documentStore.OpenAsyncSession(sessionOptions);
                                        createdSessions.Add(session);
                                        return session;
                                    });
                                })
                                .When((bus, c) =>
                                {
                                    var sendOptions = new SendOptions();

                                    sendOptions.RouteToThisEndpoint();

                                    return bus.Send(new SharedRavenSessionExtensions.GenericMessage(), sendOptions);
                                }))
                        .Done(c => c.HandlerWasHit)
                        .Run();

                var injectedSessionId = ((InMemoryDocumentSessionOperations)context.RavenSessionFromHandler).Id;
                var found = createdSessions.Cast<InMemoryDocumentSessionOperations>().Any(session => session.Id == injectedSessionId);
                Assert.That(found, Is.True);
            }
            finally
            {
                foreach (var session in createdSessions)
                {
                    session.Dispose();
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

                Assert.That(context.RavenSessionFromHandler, Is.Not.Null);
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
            public IAsyncDocumentSession RavenSessionFromTest { get; set; }
            public IAsyncDocumentSession RavenSessionFromHandler { get; set; }
            public bool HandlerWasHit { get; set; }
        }

        public class SharedRavenSessionExtensions : EndpointConfigurationBuilder
        {
            public SharedRavenSessionExtensions()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                    config.EnableOutbox();
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