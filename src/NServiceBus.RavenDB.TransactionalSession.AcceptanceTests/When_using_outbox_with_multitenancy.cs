namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_using_outbox_with_multitenancy : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_store_data_on_correct_tenant()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MultitenantEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();

                    await transactionalSession.Open(new RavenDbOpenSessionOptions(new Dictionary<string, string> { { "tenant-id", SetupFixture.TenantId } }));
                    ctx.SessionId = transactionalSession.SessionId;
                    var ravenSession = transactionalSession.SynchronizedStorageSession.RavenSession();
                    var document = new TestDocument() { SessionId = transactionalSession.SessionId };
                    await ravenSession.StoreAsync(document);

                    var sendOptions = new SendOptions();
                    sendOptions.SetHeader("tenant-id", SetupFixture.TenantId);
                    sendOptions.RouteToThisEndpoint();
                    await transactionalSession.Send(new SampleMessage { DocumentId = document.Id }, sendOptions, CancellationToken.None);

                    await transactionalSession.Commit(CancellationToken.None).ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.That(context.Document, Is.Not.Null);
            Assert.That(context.Document.SessionId, Is.EqualTo(context.SessionId), "should have loaded the document from the correct tenant database");
        }

        public class Context : ScenarioContext, IInjectServiceProvider
        {
            public IServiceProvider ServiceProvider { get; set; }

            public bool MessageReceived { get; set; }
            public TestDocument Document { get; set; }
            public string SessionId { get; set; }
        }

        public class MultitenantEndpoint : EndpointConfigurationBuilder
        {
            public MultitenantEndpoint() => EndpointSetup<TransactionSessionWithOutboxEndpoint>();

            public class SampleMessageHandler : IHandleMessages<SampleMessage>
            {
                public SampleMessageHandler(Context testContext) => this.testContext = testContext;

                public async Task Handle(SampleMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;
                    var ravenSession = context.SynchronizedStorageSession.RavenSession();
                    testContext.Document = await ravenSession.LoadAsync<TestDocument>(message.DocumentId);
                }

                readonly Context testContext;
            }
        }

        public class SampleMessage : IMessage
        {
            public string DocumentId { get; set; }
        }

        public class TestDocument
        {
            public string Id { get; set; }
            public string SessionId { get; set; }
        }
    }
}