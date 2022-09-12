namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Raven.Client.Documents.Session;

    public class When_using_transactional_session : NServiceBusAcceptanceTest
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_messages_and_store_document_in_synchronized_session_on_transactional_session_commit(bool outboxEnabled)
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                    await transactionalSession.Open(new RavenDbOpenSessionOptions());

                    await transactionalSession.SendLocal(new SampleMessage(), CancellationToken.None);

                    var ravenSession = transactionalSession.SynchronizedStorageSession.RavenSession();
                    var document = new TestDocument { Id = ctx.SessionId = transactionalSession.SessionId };
                    await ravenSession.StoreAsync(document);

                    await transactionalSession.Commit(CancellationToken.None).ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            var documents = SetupFixture.DocumentStore.OpenSession(SetupFixture.DefaultDatabaseName)
                .Query<TestDocument>()
                .Where(d => d.Id == context.SessionId);
            Assert.AreEqual(1, documents.Count());
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_messages_and_store_document_in_raven_session_on_transactional_session_commit(bool outboxEnabled)
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                    await transactionalSession.Open(new RavenDbOpenSessionOptions());

                    await transactionalSession.SendLocal(new SampleMessage(), CancellationToken.None);

                    var ravenSession = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
                    var document = new TestDocument { Id = ctx.SessionId = transactionalSession.SessionId };
                    await ravenSession.StoreAsync(document);

                    await transactionalSession.Commit(CancellationToken.None).ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            var documents = SetupFixture.DocumentStore.OpenSession(SetupFixture.DefaultDatabaseName)
                .Query<TestDocument>()
                .Where(d => d.Id == context.SessionId);
            Assert.AreEqual(1, documents.Count());
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_not_send_messages_if_session_is_not_committed(bool outboxEnabled)
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (statelessSession, ctx) =>
                {
                    using (var scope = ctx.ServiceProvider.CreateScope())
                    using (var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>())
                    {
                        await transactionalSession.Open(new RavenDbOpenSessionOptions());

                        var ravenSession = transactionalSession.SynchronizedStorageSession.RavenSession();
                        var document = new TestDocument { Id = ctx.SessionId = transactionalSession.SessionId };
                        await ravenSession.StoreAsync(document);

                        await transactionalSession.SendLocal(new SampleMessage());
                    }

                    //Send immediately dispatched message to finish the test
                    await statelessSession.SendLocal(new CompleteTestMessage());
                }))
                .Done(c => c.CompleteMessageReceived)
                .Run();

            Assert.True(context.CompleteMessageReceived);
            Assert.False(context.MessageReceived);

            var documents = SetupFixture.DocumentStore.OpenSession(SetupFixture.DefaultDatabaseName)
                .Query<TestDocument>()
                .Where(d => d.Id == context.SessionId);
            var d = documents.FirstOrDefault();
            Assert.IsEmpty(documents);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_immediate_dispatch_messages_even_if_session_is_not_committed(bool outboxEnabled)
        {
            var result = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();

                    await transactionalSession.Open(new RavenDbOpenSessionOptions());

                    var sendOptions = new SendOptions();
                    sendOptions.RequireImmediateDispatch();
                    sendOptions.RouteToThisEndpoint();
                    await transactionalSession.Send(new SampleMessage(), sendOptions, CancellationToken.None);
                }))
                .Done(c => c.MessageReceived)
                .Run()
                ;

            Assert.True(result.MessageReceived);
        }

        class Context : ScenarioContext, IInjectServiceProvider
        {
            public bool MessageReceived { get; set; }
            public bool CompleteMessageReceived { get; set; }
            public IServiceProvider ServiceProvider { get; set; }
            public string SessionId { get; set; }
        }

        class AnEndpoint : EndpointConfigurationBuilder
        {
            public AnEndpoint()
            {
                if ((bool)TestContext.CurrentContext.Test.Arguments[0]!)
                {
                    EndpointSetup<TransactionSessionDefaultServer>();
                }
                else
                {
                    EndpointSetup<TransactionSessionWithOutboxEndpoint>();
                }
            }

            class SampleHandler : IHandleMessages<SampleMessage>
            {
                public SampleHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(SampleMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }

            class CompleteTestMessageHandler : IHandleMessages<CompleteTestMessage>
            {
                public CompleteTestMessageHandler(Context context) => testContext = context;

                public Task Handle(CompleteTestMessage message, IMessageHandlerContext context)
                {
                    testContext.CompleteMessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }
        }

        class SampleMessage : ICommand
        {
        }

        class CompleteTestMessage : ICommand
        {
        }

        public class TestDocument
        {
            public string Id { get; set; }
        }
    }
}