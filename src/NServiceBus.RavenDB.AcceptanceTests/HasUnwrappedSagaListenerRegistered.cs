namespace NServiceBus.RavenDB.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;

    // TODO: Expand to create previous-version data like When_loading_Raven3_sagas unit test
    public class HasUnwrappedSagaListenerRegistered : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task IsRegistered()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaAndOutbox>(b => b.When((session, ctx) => session.SendLocal(new TestMsg())))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.IsTrue(context.IsRegistered, "Endpoint initialization is not registering UnwrappedSagaListener - sagas from before RavenDB 4.0 will not be able to be loaded.");
        }

        public class Context : ScenarioContext
        {
            public bool IsRegistered { get; set; }
            public bool MessageReceived { get; set; }
        }

        public class EndpointWithSagaAndOutbox : EndpointConfigurationBuilder
        {
            public EndpointWithSagaAndOutbox()
            {
                EndpointSetup<DefaultServer>();
            }

            class BoringHandler : IHandleMessages<TestMsg>
            {
                Context testContext;

                public BoringHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(TestMsg message, IMessageHandlerContext context)
                {
                    var session = context.SynchronizedStorageSession.RavenSession();
                    var store = session.Advanced.DocumentStore as DocumentStore;

                    testContext.IsRegistered = ListenerIsRegistered(store);
                    testContext.MessageReceived = true;

                    return Task.CompletedTask;
                }
            }

            // It's a bit of a hack to do this by reflection, but there's just no other way (other than complicated smoke test) to ensure that this behavior is inserted
            static bool ListenerIsRegistered(IDocumentStore store)
            {
                var eventField = typeof(DocumentStoreBase).GetField("OnBeforeConversionToEntity", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);

                if (eventField.GetValue(store) is not EventHandler<BeforeConversionToEntityEventArgs> eventDelegate)
                {
                    return false;
                }

                foreach (var existingHandler in eventDelegate.GetInvocationList())
                {
                    if (existingHandler.Method.DeclaringType.Name == "UnwrappedSagaListener")
                    {
                        return true;
                    }
                }

                return false;
            }

            public class OrderSagaData : ContainSagaData
            {
                public string OrderId { get; set; }
                public int ContinueCount { get; set; }
                public List<int> CollectedIndexes { get; set; } = [];
            }
        }

        public class TestMsg : ICommand
        {
            public string OrderId { get; set; }
        }
    }
}
